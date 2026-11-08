using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Marten;
using Marten.Events;
using Npgsql;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Infrastructure.Administration;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Api;

[Collection("Backend database integration")]
public sealed class AdministratorAuthenticationTests : IAsyncLifetime
{
    private readonly AdministratorAuthenticationFactory factory = new();

    public async ValueTask InitializeAsync()
    {
        await factory.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await factory.DisposeAsync();
    }

    private async Task CleanupBeforeTestAsync()
    {
        var store = factory.Services.GetRequiredService<IDocumentStore>();
        await store.Advanced.Clean.DeleteAllEventDataAsync();
        await store.Advanced.Clean.DeleteDocumentsByTypeAsync(typeof(AdministratorAccountDocument));
        await store.Advanced.Clean.DeleteDocumentsByTypeAsync(typeof(AdministratorSessionDocument));
        await store.Advanced.Clean.DeleteDocumentsByTypeAsync(typeof(AdministratorGrantDocument));
        await store.Advanced.Clean.DeleteDocumentsByTypeAsync(typeof(AdministratorAuditDocument));
    }

    [Fact]
    public async Task BootstrapCreatesFirstAdministratorAccount()
    {
        var service = factory.Services.GetRequiredService<IAdministratorAuthenticationService>();
        var credential = await service.IssueBootstrapCredentialAsync(TestContext.Current.CancellationToken);
        await factory.WaitForProjectionsAsync();

        var result = await service.BootstrapAsync(credential, "local-admin", "local-admin-password-1", TestContext.Current.CancellationToken);

        Assert.Equal(AdministratorOperationResult.Succeeded, result);
    }

    [Fact]
    public async Task BootstrapRejectsShortPassword()
    {
        var service = factory.Services.GetRequiredService<IAdministratorAuthenticationService>();
        var credential = await service.IssueBootstrapCredentialAsync(TestContext.Current.CancellationToken);
        await factory.WaitForProjectionsAsync();

        var result = await service.BootstrapAsync(credential, "local-admin", "short", TestContext.Current.CancellationToken);

        Assert.Equal(AdministratorOperationResult.InvalidGrant, result);
    }

    [Fact]
    public async Task BootstrapCredentialIsStoredAsHash()
    {
        var service = factory.Services.GetRequiredService<IAdministratorAuthenticationService>();
        var credential = await service.IssueBootstrapCredentialAsync(TestContext.Current.CancellationToken);

        await factory.WaitForProjectionsAsync();

        var grant = await factory.LoadGrantDocumentAsync();

        Assert.NotNull(grant);
        Assert.NotEqual(credential, grant!.TokenHash);
    }

    [Fact]
    public async Task ExpiredBootstrapCredentialIsRejected()
    {
        var service = factory.Services.GetRequiredService<IAdministratorAuthenticationService>();
        var credential = await service.IssueBootstrapCredentialAsync(TestContext.Current.CancellationToken);

        await factory.WaitForProjectionsAsync();
        await AdministratorAuthenticationFactory.ExpireCurrentGrantAsync();

        var result = await service.BootstrapAsync(credential, "local-admin", "local-admin-password-1", TestContext.Current.CancellationToken);

        Assert.Equal(AdministratorOperationResult.InvalidGrant, result);
    }

    [Fact]
    public async Task ConcurrentBootstrapConsumesCredentialExactlyOnce()
    {
        var service = factory.Services.GetRequiredService<IAdministratorAuthenticationService>();
        var credential = await service.IssueBootstrapCredentialAsync(TestContext.Current.CancellationToken);
        await factory.WaitForProjectionsAsync();

        var results = await Task.WhenAll(
            service.BootstrapAsync(credential, "local-admin", "local-admin-password-1", TestContext.Current.CancellationToken),
            service.BootstrapAsync(credential, "local-admin", "local-admin-password-1", TestContext.Current.CancellationToken));

        Assert.Equal(
            new[] { AdministratorOperationResult.Succeeded, AdministratorOperationResult.InvalidGrant }.OrderBy(x => x),
            results.OrderBy(x => x));
    }

    [Fact]
    public async Task SignInReturnsSessionToken()
    {
        using var client = factory.CreateClient();
        await factory.BootstrapAdministratorAsync();

        using var response = await client.PostAsync("/local/api/v1/admin/session",
            new StringContent(JsonSerializer.Serialize(new { userName = "local-admin", password = "local-admin-password-1" }),
                Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Authorization", out var values));
        Assert.NotEmpty(values);
    }

    [Fact]
    public async Task SignInWithWrongPasswordReturnsUnauthorized()
    {
        using var client = factory.CreateClient();
        await factory.BootstrapAdministratorAsync();

        using var response = await client.PostAsync("/local/api/v1/admin/session",
            new StringContent(JsonSerializer.Serialize(new { userName = "local-admin", password = "wrong-password" }),
                Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SessionTokenIsStoredAsHash()
    {
        using var client = factory.CreateClient();
        await factory.BootstrapAdministratorAsync();

        using var response = await client.PostAsync("/local/api/v1/admin/session",
            new StringContent(JsonSerializer.Serialize(new { userName = "local-admin", password = "local-admin-password-1" }),
                Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var token = AuthenticationHeaderValue.Parse(response.Headers.GetValues("Authorization").Single()).Parameter!;

        await factory.WaitForProjectionsAsync();

        var sessions = await factory.LoadSessionDocumentsAsync();

        Assert.Single(sessions);
        Assert.NotEqual(token, sessions[0].TokenHash);
    }

    [Fact]
    public async Task RecoverySucceedsWithValidCredential()
    {
        await factory.BootstrapAdministratorAsync();
        var service = factory.Services.GetRequiredService<IAdministratorAuthenticationService>();
        var credential = await service.IssueRecoveryCredentialAsync(TestContext.Current.CancellationToken);
        await factory.WaitForProjectionsAsync();

        var result = await service.RecoverAsync(credential, "local-admin", "replacement-password-1", TestContext.Current.CancellationToken);

        Assert.Equal(AdministratorOperationResult.Succeeded, result);
    }

    [Fact]
    public async Task ExpiredRecoveryCredentialIsRejected()
    {
        await factory.BootstrapAdministratorAsync();
        var service = factory.Services.GetRequiredService<IAdministratorAuthenticationService>();
        var credential = await service.IssueRecoveryCredentialAsync(TestContext.Current.CancellationToken);

        await factory.WaitForProjectionsAsync();
        await AdministratorAuthenticationFactory.ExpireCurrentGrantAsync();

        var result = await service.RecoverAsync(credential, "local-admin", "replacement-password-1", TestContext.Current.CancellationToken);

        Assert.Equal(AdministratorOperationResult.InvalidGrant, result);
    }

    [Fact]
    public async Task RecoveryRevokesExistingSessions()
    {
        using var client = factory.CreateClient();
        await factory.BootstrapAdministratorAsync();
        var token = await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");
        await factory.RecoverAdministratorAsync();

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, "/local/api/v1/admin/accounts", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RecoveryReplacesPassword()
    {
        using var client = factory.CreateClient();
        await factory.BootstrapAdministratorAsync();
        await factory.RecoverAdministratorAsync();

        using var response = await client.PostAsync("/local/api/v1/admin/session",
            new StringContent(JsonSerializer.Serialize(new { userName = "local-admin", password = "replacement-password-1" }),
                Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RecoveryCredentialCannotBeReplayed()
    {
        await factory.BootstrapAdministratorAsync();
        var credential = await factory.RecoverAdministratorAsync();
        var service = factory.Services.GetRequiredService<IAdministratorAuthenticationService>();

        var result = await service.RecoverAsync(credential, "local-admin", "another-replacement-password-1", TestContext.Current.CancellationToken);

        Assert.Equal(AdministratorOperationResult.InvalidGrant, result);
    }

    [Fact]
    public async Task LogoutRevokesSession()
    {
        using var client = factory.CreateClient();
        await factory.BootstrapAdministratorAsync();
        await factory.RecoverAdministratorAsync();
        var token = await AdministratorAuthenticationFactory.SignInAsync(client, "replacement-password-1");

        using var logout = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Delete, "/local/api/v1/admin/session", token);
        logout.EnsureSuccessStatusCode();

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, "/local/api/v1/admin/accounts", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RenewalReturnsNewToken()
    {
        using var client = factory.CreateClient();
        await factory.BootstrapAdministratorAsync();
        var token = await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Post, "/local/api/v1/admin/session/renew", token);
        response.EnsureSuccessStatusCode();
        var newToken = AuthenticationHeaderValue.Parse(response.Headers.GetValues("Authorization").Single()).Parameter!;

        Assert.NotEqual(token, newToken);
    }

    [Fact]
    public async Task RenewalRevokesPreviousSession()
    {
        using var client = factory.CreateClient();
        await factory.BootstrapAdministratorAsync();
        var token = await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");
        await AdministratorAuthenticationFactory.RenewSessionAsync(client, token);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, "/local/api/v1/admin/accounts", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReaderCanViewAccounts()
    {
        using var client = factory.CreateClient();
        var readerId = await factory.CreateReaderAccountAsync(client);
        var readerToken = await AdministratorAuthenticationFactory.SignInReaderAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, "/local/api/v1/admin/accounts", readerToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReaderCannotCreateAccounts()
    {
        using var client = factory.CreateClient();
        await factory.CreateReaderAccountAsync(client);
        var readerToken = await AdministratorAuthenticationFactory.SignInReaderAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, "/local/api/v1/admin/accounts", readerToken,
            new { userName = "denied-user", password = "denied-user-password", role = AdministratorRoles.Reader });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReaderCannotChangeRoles()
    {
        using var client = factory.CreateClient();
        var readerId = await factory.CreateReaderAccountAsync(client);
        var readerToken = await AdministratorAuthenticationFactory.SignInReaderAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Put,
            $"/local/api/v1/admin/accounts/{readerId:D}/role", readerToken, new { role = AdministratorRoles.Administrator });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task LastAdministratorCannotBeDemoted()
    {
        using var client = factory.CreateClient();
        await factory.BootstrapAdministratorAsync();
        var token = await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");

        await factory.WaitForProjectionsAsync();
        var accounts = await factory.LoadAccountDocumentsAsync();
        var adminId = accounts[0].Id;

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Put,
            $"/local/api/v1/admin/accounts/{adminId:D}/role", token, new { role = AdministratorRoles.Reader });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousRequestsAreUnauthorized()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/local/api/v1/admin/accounts", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RemoteAuthenticationRequiresHttps()
    {
        using var client = factory.CreateClient();
        await factory.BootstrapAdministratorAsync();
        var token = await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");

        using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/local/api/v1/admin/accounts");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Test-Remote", "true");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PasswordIsStoredAsVerifiableHash()
    {
        await factory.BootstrapAdministratorAsync();
        await factory.WaitForProjectionsAsync();

        var accounts = await factory.LoadAccountDocumentsAsync();

        Assert.Single(accounts);
        var account = accounts[0];
        Assert.NotEqual("local-admin-password-1", account.PasswordHash);
        Assert.False(account.PasswordHash.Contains("local-admin-password-1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AuditEventRecordsActor()
    {
        using var client = factory.CreateClient();
        var (token, readerId) = await factory.PrepareReaderWithTokenAsync(client);

        await factory.WaitForProjectionsAsync();
        var adminAccounts = await factory.LoadAccountDocumentsAsync();
        var adminId = adminAccounts.FirstOrDefault(a => a.Role == AdministratorRoles.Administrator)?.Id
            ?? throw new InvalidOperationException("No admin account found");

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Put,
            $"/local/api/v1/admin/accounts/{readerId:D}/role", token, new { role = AdministratorRoles.Administrator });
        response.EnsureSuccessStatusCode();

        await factory.WaitForProjectionsAsync();
        var audits = await factory.LoadAuditDocumentsAsync();
        var roleChangeAudit = audits.FirstOrDefault(a => a.TargetAccountId == readerId && a.EventType == "AdministratorRoleChanged");

        Assert.NotNull(roleChangeAudit);
        Assert.Equal(adminId, roleChangeAudit!.ActorAccountId);
    }

    private static string HashSecret(string secret) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
}

public sealed class AdministratorAuthenticationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static string BaseConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__EventStore") ??
        "Host=localhost;Port=5432;Database=scifilarnetwork_tests;Username=postgres;Password=postgres";

    public CapturingLoggerProvider LogMessages { get; } = new();

    public async ValueTask InitializeAsync()
    {
        var store = Services.GetRequiredService<IDocumentStore>();
        await store.Advanced.Clean.DeleteAllEventDataAsync();
        await store.Advanced.Clean.DeleteDocumentsByTypeAsync(typeof(AdministratorAccountDocument));
        await store.Advanced.Clean.DeleteDocumentsByTypeAsync(typeof(AdministratorSessionDocument));
        await store.Advanced.Clean.DeleteDocumentsByTypeAsync(typeof(AdministratorGrantDocument));
        await store.Advanced.Clean.DeleteDocumentsByTypeAsync(typeof(AdministratorAuditDocument));
    }

    public new async ValueTask DisposeAsync()
    {
        var store = Services.GetRequiredService<IDocumentStore>();
        await store.Advanced.Clean.DeleteAllEventDataAsync();
        await store.Advanced.Clean.DeleteDocumentsByTypeAsync(typeof(AdministratorAccountDocument));
        await store.Advanced.Clean.DeleteDocumentsByTypeAsync(typeof(AdministratorSessionDocument));
        await store.Advanced.Clean.DeleteDocumentsByTypeAsync(typeof(AdministratorGrantDocument));
        await store.Advanced.Clean.DeleteDocumentsByTypeAsync(typeof(AdministratorAuditDocument));
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("PathBase", "/local");
        builder.UseSetting("ConnectionStrings:EventStore", BaseConnectionString);
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddProvider(LogMessages);
        });
        builder.ConfigureTestServices(services => services.AddSingleton<IStartupFilter, LoopbackAddressStartupFilter>());
    }

    public async Task WaitForProjectionsAsync()
    {
        var store = Services.GetRequiredService<IDocumentStore>();
        await store.WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(10));
    }

    public async Task<IReadOnlyList<AdministratorAccountDocument>> LoadAccountDocumentsAsync()
    {
        await using var session = Services.GetRequiredService<IDocumentStore>().QuerySession();
        return await session.Query<AdministratorAccountDocument>().ToListAsync();
    }

    public async Task<IReadOnlyList<AdministratorSessionDocument>> LoadSessionDocumentsAsync()
    {
        await using var session = Services.GetRequiredService<IDocumentStore>().QuerySession();
        return await session.Query<AdministratorSessionDocument>().ToListAsync();
    }

    public async Task<AdministratorGrantDocument?> LoadGrantDocumentAsync()
    {
        await using var session = Services.GetRequiredService<IDocumentStore>().QuerySession();
        return await session.Query<AdministratorGrantDocument>().FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<AdministratorAuditDocument>> LoadAuditDocumentsAsync()
    {
        await using var session = Services.GetRequiredService<IDocumentStore>().QuerySession();
        return await session.Query<AdministratorAuditDocument>().ToListAsync();
    }

    public static async Task ExpireCurrentGrantAsync()
    {
        await using var connection = new NpgsqlConnection(BaseConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "UPDATE public.mt_events SET data = jsonb_set(data::jsonb, '{expiresAtUtc}', " +
            "'\"1970-01-01T00:00:00Z\"') WHERE data::text LIKE '%CredentialIssued%'",
            connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task BootstrapAdministratorAsync()
    {
        var service = Services.GetRequiredService<IAdministratorAuthenticationService>();
        var credential = await service.IssueBootstrapCredentialAsync(TestContext.Current.CancellationToken);
        await WaitForProjectionsAsync();
        var result = await service.BootstrapAsync(credential, "local-admin", "local-admin-password-1", TestContext.Current.CancellationToken);
        if (result != AdministratorOperationResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to bootstrap administrator: {result}");
        }
        await WaitForProjectionsAsync();
    }

    public async Task<string> RecoverAdministratorAsync()
    {
        var service = Services.GetRequiredService<IAdministratorAuthenticationService>();
        var credential = await service.IssueRecoveryCredentialAsync(TestContext.Current.CancellationToken);
        await WaitForProjectionsAsync();
        var result = await service.RecoverAsync(credential, "local-admin", "replacement-password-1", TestContext.Current.CancellationToken);
        if (result != AdministratorOperationResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to recover administrator: {result}");
        }
        await WaitForProjectionsAsync();
        return credential;
    }

    public static async Task<string> SignInAsync(HttpClient client, string password)
    {
        using var response = await client.PostAsync("/local/api/v1/admin/session",
            new StringContent(JsonSerializer.Serialize(new { userName = "local-admin", password }),
                Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return AuthenticationHeaderValue.Parse(response.Headers.GetValues("Authorization").Single()).Parameter!;
    }

    public static async Task<string> RenewSessionAsync(HttpClient client, string token)
    {
        using var response = await SendAuthorizedAsync(client, HttpMethod.Post, "/local/api/v1/admin/session/renew", token);
        response.EnsureSuccessStatusCode();
        return AuthenticationHeaderValue.Parse(response.Headers.GetValues("Authorization").Single()).Parameter!;
    }

    public async Task<Guid> CreateReaderAccountAsync(HttpClient client)
    {
        await BootstrapAdministratorAsync();
        var token = await SignInAsync(client, "local-admin-password-1");

        using var response = await SendAuthorizedJsonAsync(client, HttpMethod.Post, "/local/api/v1/admin/accounts", token,
            new { userName = "auditor", password = "reader-account-password-1", role = AdministratorRoles.Reader });
        response.EnsureSuccessStatusCode();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return body.RootElement.GetProperty("properties").GetProperty("accountId").GetGuid();
    }

    public async Task<(string Token, Guid ReaderId)> PrepareReaderWithTokenAsync(HttpClient client)
    {
        await BootstrapAdministratorAsync();
        var token = await SignInAsync(client, "local-admin-password-1");
        var readerId = await AdministratorAuthenticationFactory.CreateReaderAccountInternalAsync(client, token);
        return (token, readerId);
    }

    private static async Task<Guid> CreateReaderAccountInternalAsync(HttpClient client, string token)
    {
        using var response = await SendAuthorizedJsonAsync(client, HttpMethod.Post, "/local/api/v1/admin/accounts", token,
            new { userName = "auditor", password = "reader-account-password-1", role = AdministratorRoles.Reader });
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return body.RootElement.GetProperty("properties").GetProperty("accountId").GetGuid();
    }

    public static async Task<string> SignInReaderAsync(HttpClient client)
    {
        using var response = await client.PostAsync("/local/api/v1/admin/session",
            new StringContent(JsonSerializer.Serialize(new { userName = "auditor", password = "reader-account-password-1" }),
                Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return AuthenticationHeaderValue.Parse(response.Headers.GetValues("Authorization").Single()).Parameter!;
    }

    public static async Task<HttpResponseMessage> SendAuthorizedJsonAsync(
        HttpClient client, HttpMethod method, string path, string token, object payload)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd("application/vnd.siren+json");
        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpClient client, HttpMethod method, string path, string token)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd("application/vnd.siren+json");
        return await client.SendAsync(request);
    }
}

public sealed class CapturingLoggerProvider : ILoggerProvider
{
    public ConcurrentBag<string> Messages { get; } = [];

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);

    public void Dispose() { }

    private sealed class CapturingLogger(ConcurrentBag<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => messages.Add(formatter(state, exception));
    }
}

internal sealed class LoopbackAddressStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => application =>
    {
        application.Use(async (context, nextRequest) =>
        {
            context.Connection.RemoteIpAddress = context.Request.Headers.ContainsKey("X-Test-Remote")
                ? IPAddress.Parse("192.0.2.1")
                : IPAddress.Loopback;
            await nextRequest();
        });
        next(application);
    };
}
