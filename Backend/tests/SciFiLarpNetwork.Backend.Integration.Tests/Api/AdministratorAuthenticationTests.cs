using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Infrastructure.Administration;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Api;

[Collection("Backend database integration")]
public sealed class AdministratorAuthenticationTests(
    AdministratorAuthenticationFactory factory) : IClassFixture<AdministratorAuthenticationFactory>
{
    [Fact]
    public async Task ConcurrentFirstUseInitializesAuthenticationSchemaSafely()
    {
        using var dataSource = NpgsqlDataSource.Create(factory.ConnectionString);
        using var authenticationService = new AdministratorAuthenticationService(
            dataSource,
            new PasswordHasher<object>());

        var credentials = await Task.WhenAll(
            Enumerable.Range(0, 8)
                .Select(_ => authenticationService.IssueBootstrapCredentialAsync()));

        Assert.All(credentials, credential => Assert.False(string.IsNullOrWhiteSpace(credential)));
    }

    [Fact]
    public async Task BootstrapSessionsRolesRecoveryAndAuditingAreSecure()
    {
        using var client = factory.CreateClient();
        var authenticationService = factory.Services.GetRequiredService<IAdministratorAuthenticationService>();
        await PrepareEmptyAuthenticationStoreAsync(authenticationService);

        try
        {
            using (var anonymousResponse = await client.GetAsync("/local/api/v1/admin/accounts"))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
                await AssertSirenError(anonymousResponse, "errors.unauthorized");
            }

            using (var anonymousCreate = await PostJsonAsync(client, "/local/api/v1/admin/accounts", new
                   {
                       userName = "unauthorized",
                       password = "unauthorized-password",
                       role = AdministratorRoles.Reader
                   }))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, anonymousCreate.StatusCode);
                await AssertSirenError(anonymousCreate, "errors.unauthorized");
            }

            using (var anonymousRoleChange = await client.PutAsync(
                       $"/local/api/v1/admin/accounts/{Guid.NewGuid():D}/role",
                       new StringContent("""{"role":"administrator"}""", Encoding.UTF8, "application/json")))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, anonymousRoleChange.StatusCode);
                await AssertSirenError(anonymousRoleChange, "errors.unauthorized");
            }

            using (var noPublicBootstrap = await PostJsonAsync(client, "/local/api/v1/admin/bootstrap", new { }))
            {
                Assert.Equal(HttpStatusCode.NotFound, noPublicBootstrap.StatusCode);
            }

            using (var noPublicRecovery = await PostJsonAsync(client, "/local/api/v1/admin/recovery", new { }))
            {
                Assert.Equal(HttpStatusCode.NotFound, noPublicRecovery.StatusCode);
            }

            var expiredBootstrapCredential = await authenticationService.IssueBootstrapCredentialAsync();
            await ExecuteSqlAsync(
                "UPDATE scifi_administrator_grants SET expires_at_utc = NOW() - INTERVAL '1 minute' WHERE id = 1");
            var expiredBootstrapResult = await authenticationService.BootstrapAsync(
                expiredBootstrapCredential,
                "local-admin",
                "local-admin-password-1");
            Assert.Equal(AdministratorOperationResult.InvalidGrant, expiredBootstrapResult);

            var bootstrapCredential = await authenticationService.IssueBootstrapCredentialAsync();
            var bootstrapCredentialHash = await ReadScalarAsync<string>(
                "SELECT token_hash FROM scifi_administrator_grants WHERE id = 1");
            Assert.NotEqual(bootstrapCredential, bootstrapCredentialHash);
            const string administratorPassword = "local-admin-password-1";
            var bootstrapResponses = await Task.WhenAll(
                authenticationService.BootstrapAsync(bootstrapCredential, "local-admin", administratorPassword),
                authenticationService.BootstrapAsync(bootstrapCredential, "local-admin", administratorPassword));
            Assert.Single(bootstrapResponses, result => result == AdministratorOperationResult.Succeeded);
            Assert.Single(bootstrapResponses, result => result == AdministratorOperationResult.InvalidGrant);

            using (var forgedSignIn = await PostJsonAsync(client, "/local/api/v1/admin/session", new
                   {
                       userName = "local-admin",
                       password = administratorPassword,
                       role = AdministratorRoles.Administrator
                   }))
            {
                Assert.Equal(HttpStatusCode.BadRequest, forgedSignIn.StatusCode);
                await AssertSirenError(forgedSignIn, "errors.validation");
            }

            using (var invalidSignIn = await PostJsonAsync(client, "/local/api/v1/admin/session", new
                   {
                       userName = "local-admin",
                       password = "incorrect-password"
                   }))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, invalidSignIn.StatusCode);
                await AssertSirenError(invalidSignIn, "errors.unauthorized");
            }

            using var administratorSignIn = await PostJsonAsync(client, "/local/api/v1/admin/session", new
            {
                userName = "LOCAL-ADMIN",
                password = administratorPassword
            });
            Assert.Equal(HttpStatusCode.OK, administratorSignIn.StatusCode);
            var administratorToken = AssertBearerToken(administratorSignIn);
            var administratorSessionBody = await administratorSignIn.Content.ReadAsStringAsync();
            Assert.DoesNotContain(administratorToken, administratorSessionBody, StringComparison.Ordinal);
            Assert.DoesNotContain(administratorPassword, administratorSessionBody, StringComparison.Ordinal);
            using var administratorSessionDocument = JsonDocument.Parse(administratorSessionBody);
            var administratorId = administratorSessionDocument.RootElement
                .GetProperty("properties")
                .GetProperty("accountId")
                .GetGuid();
            Assert.Contains(
                administratorSessionDocument.RootElement.GetProperty("links").EnumerateArray(),
                link => link.GetProperty("href").GetString() == "/local/api/v1/admin/session" &&
                        link.GetProperty("rel").EnumerateArray().Any(relation => relation.GetString() == "self"));
            Assert.Contains(
                administratorSessionDocument.RootElement.GetProperty("actions").EnumerateArray(),
                action => action.GetProperty("name").GetString() == "administrator-renew-session");

            using (var anonymousDiscovery = await client.GetAsync("/local/api/v1"))
            using (var anonymousDiscoveryBody = JsonDocument.Parse(await anonymousDiscovery.Content.ReadAsStringAsync()))
            {
                Assert.DoesNotContain(
                    anonymousDiscoveryBody.RootElement.GetProperty("links").EnumerateArray(),
                    link => link.GetProperty("href").GetString() == "/local/api/v1/admin/accounts");
                Assert.Contains(
                    anonymousDiscoveryBody.RootElement.GetProperty("actions").EnumerateArray(),
                    action => action.GetProperty("name").GetString() == "administrator-sign-in" &&
                              action.GetProperty("href").GetString() == "/local/api/v1/admin/session");
            }

            using (var authenticatedDiscovery = await SendAuthorizedAsync(
                       client,
                       HttpMethod.Get,
                       "/local/api/v1",
                       administratorToken))
            using (var authenticatedDiscoveryBody = JsonDocument.Parse(await authenticatedDiscovery.Content.ReadAsStringAsync()))
            {
                Assert.Contains(
                    authenticatedDiscoveryBody.RootElement.GetProperty("links").EnumerateArray(),
                    link => link.GetProperty("href").GetString() == "/local/api/v1/admin/accounts");
                Assert.False(authenticatedDiscoveryBody.RootElement.TryGetProperty("actions", out _));
            }

            using (var remoteLoginRequest = new HttpRequestMessage(HttpMethod.Post, "/local/api/v1/admin/session")
                   {
                       Content = new StringContent(
                           JsonSerializer.Serialize(new { userName = "local-admin", password = administratorPassword }),
                           Encoding.UTF8,
                           "application/json")
                   })
            {
                remoteLoginRequest.Headers.Add("X-Test-Remote", "true");
                using var remoteLogin = await client.SendAsync(remoteLoginRequest);
                Assert.Equal(HttpStatusCode.Forbidden, remoteLogin.StatusCode);
            }

            using (var remoteSessionRequest = new HttpRequestMessage(HttpMethod.Get, "/local/api/v1/admin/accounts"))
            {
                remoteSessionRequest.Headers.Add("X-Test-Remote", "true");
                remoteSessionRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", administratorToken);
                using var remoteSession = await client.SendAsync(remoteSessionRequest);
                Assert.Equal(HttpStatusCode.Unauthorized, remoteSession.StatusCode);
            }

            using (var secureRemoteRequest = new HttpRequestMessage(HttpMethod.Get, "https://localhost/local/api/v1/admin/accounts"))
            {
                secureRemoteRequest.Headers.Add("X-Test-Remote", "true");
                secureRemoteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", administratorToken);
                using var secureRemoteResponse = await client.SendAsync(secureRemoteRequest);
                Assert.Equal(HttpStatusCode.OK, secureRemoteResponse.StatusCode);
            }

            var passwordHash = await ReadScalarAsync<string>(
                "SELECT password_hash FROM scifi_administrator_accounts WHERE id = @accountId",
                new NpgsqlParameter("accountId", administratorId));
            Assert.NotEqual(administratorPassword, passwordHash);
            Assert.DoesNotContain(administratorPassword, passwordHash, StringComparison.Ordinal);
            Assert.Equal(
                PasswordVerificationResult.Success,
                new PasswordHasher<object>().VerifyHashedPassword(new object(), passwordHash, administratorPassword));
            var persistedSessionHash = await ReadScalarAsync<string>(
                "SELECT token_hash FROM scifi_administrator_sessions WHERE account_id = @accountId",
                new NpgsqlParameter("accountId", administratorId));
            Assert.NotEqual(administratorToken, persistedSessionHash);

            using (var directoryResponse = await SendAuthorizedAsync(
                       client,
                       HttpMethod.Get,
                       "/local/api/v1/admin/accounts",
                       administratorToken))
            {
                Assert.Equal(HttpStatusCode.OK, directoryResponse.StatusCode);
                using var directoryBody = JsonDocument.Parse(await directoryResponse.Content.ReadAsStringAsync());
                Assert.Contains(
                    directoryBody.RootElement.GetProperty("actions").EnumerateArray(),
                    action => action.GetProperty("name").GetString() == "create-administrator");
            }

            const string readerPassword = "reader-account-password-1";
            using (var forgedCreation = await SendAuthorizedJsonAsync(
                       client,
                       HttpMethod.Post,
                       "/local/api/v1/admin/accounts",
                       administratorToken,
                       new
                       {
                           userName = "auditor",
                           password = readerPassword,
                           role = AdministratorRoles.Reader,
                           actorAccountId = Guid.Empty
                       }))
            {
                Assert.Equal(HttpStatusCode.BadRequest, forgedCreation.StatusCode);
                await AssertSirenError(forgedCreation, "errors.validation");
            }

            using var readerCreation = await SendAuthorizedJsonAsync(
                client,
                HttpMethod.Post,
                "/local/api/v1/admin/accounts",
                administratorToken,
                new
                {
                    userName = "auditor",
                    password = readerPassword,
                    role = AdministratorRoles.Reader
                });
            Assert.Equal(HttpStatusCode.Created, readerCreation.StatusCode);
            var readerCreationBody = await readerCreation.Content.ReadAsStringAsync();
            Assert.DoesNotContain(readerPassword, readerCreationBody, StringComparison.Ordinal);
            using var readerCreationDocument = JsonDocument.Parse(readerCreationBody);
            var readerId = readerCreationDocument.RootElement.GetProperty("properties").GetProperty("accountId").GetGuid();
            Assert.Contains(
                readerCreationDocument.RootElement.GetProperty("links").EnumerateArray(),
                link => link.GetProperty("href").GetString() == $"/local/api/v1/admin/accounts/{readerId:D}" &&
                        link.GetProperty("rel").EnumerateArray().Any(relation => relation.GetString() == "self"));

            using var readerSignIn = await PostJsonAsync(client, "/local/api/v1/admin/session", new
            {
                userName = "auditor",
                password = readerPassword
            });
            Assert.Equal(HttpStatusCode.OK, readerSignIn.StatusCode);
            var readerToken = AssertBearerToken(readerSignIn);

            using (var readerDirectory = await SendAuthorizedAsync(
                       client,
                       HttpMethod.Get,
                       "/local/api/v1/admin/accounts",
                       readerToken))
            {
                Assert.Equal(HttpStatusCode.OK, readerDirectory.StatusCode);
                using var directoryBody = JsonDocument.Parse(await readerDirectory.Content.ReadAsStringAsync());
                Assert.False(directoryBody.RootElement.TryGetProperty("actions", out _));
            }

            using (var readerAccount = await SendAuthorizedAsync(
                       client,
                       HttpMethod.Get,
                       $"/local/api/v1/admin/accounts/{readerId:D}",
                       readerToken))
            {
                Assert.Equal(HttpStatusCode.OK, readerAccount.StatusCode);
                using var readerAccountBody = JsonDocument.Parse(await readerAccount.Content.ReadAsStringAsync());
                Assert.False(readerAccountBody.RootElement.TryGetProperty("actions", out _));
            }

            using (var readerWrite = await SendAuthorizedJsonAsync(
                       client,
                       HttpMethod.Put,
                       $"/local/api/v1/admin/accounts/{readerId:D}/role",
                       readerToken,
                       new { role = AdministratorRoles.Administrator }))
            {
                Assert.Equal(HttpStatusCode.Forbidden, readerWrite.StatusCode);
                await AssertSirenError(readerWrite, "errors.forbidden");
            }

            using (var readerCreate = await SendAuthorizedJsonAsync(
                       client,
                       HttpMethod.Post,
                       "/local/api/v1/admin/accounts",
                       readerToken,
                       new { userName = "denied-user", password = "denied-user-password", role = AdministratorRoles.Reader }))
            {
                Assert.Equal(HttpStatusCode.Forbidden, readerCreate.StatusCode);
                await AssertSirenError(readerCreate, "errors.forbidden");
            }

            using (var lastAdministrator = await SendAuthorizedJsonAsync(
                       client,
                       HttpMethod.Put,
                       $"/local/api/v1/admin/accounts/{administratorId:D}/role",
                       administratorToken,
                       new { role = AdministratorRoles.Reader }))
            {
                Assert.Equal(HttpStatusCode.Conflict, lastAdministrator.StatusCode);
            }

            using (var forgedRoleChange = await SendAuthorizedJsonAsync(
                       client,
                       HttpMethod.Put,
                       $"/local/api/v1/admin/accounts/{readerId:D}/role",
                       administratorToken,
                       new { role = AdministratorRoles.Administrator, actorAccountId = Guid.Empty }))
            {
                Assert.Equal(HttpStatusCode.BadRequest, forgedRoleChange.StatusCode);
                await AssertSirenError(forgedRoleChange, "errors.validation");
            }

            using (var roleChange = await SendAuthorizedJsonAsync(
                       client,
                       HttpMethod.Put,
                       $"/local/api/v1/admin/accounts/{readerId:D}/role",
                       administratorToken,
                       new { role = AdministratorRoles.Administrator }))
            {
                Assert.Equal(HttpStatusCode.OK, roleChange.StatusCode);
            }

            var auditedActorId = await ReadScalarAsync<Guid>(
                "SELECT actor_id FROM scifi_administrator_audit_events " +
                "WHERE target_account_id = @targetId AND event_type = 'AdministratorRoleChanged' " +
                "ORDER BY occurred_at_utc DESC LIMIT 1",
                new NpgsqlParameter("targetId", readerId));
            Assert.Equal(administratorId, auditedActorId);
            var creationActorId = await ReadScalarAsync<Guid>(
                "SELECT actor_id FROM scifi_administrator_audit_events " +
                "WHERE target_account_id = @targetId AND event_type = 'AdministratorAccountCreated' LIMIT 1",
                new NpgsqlParameter("targetId", readerId));
            Assert.Equal(administratorId, creationActorId);
            Assert.DoesNotContain(
                factory.LogMessages.Messages,
                message => message.Contains(bootstrapCredential, StringComparison.Ordinal) ||
                           message.Contains(administratorPassword, StringComparison.Ordinal) ||
                           message.Contains(administratorToken, StringComparison.Ordinal));

            using var renewal = await SendAuthorizedAsync(
                client,
                HttpMethod.Post,
                "/local/api/v1/admin/session/renew",
                administratorToken);
            Assert.Equal(HttpStatusCode.OK, renewal.StatusCode);
            var renewedToken = AssertBearerToken(renewal);
            Assert.NotEqual(administratorToken, renewedToken);
            using (var oldSession = await SendAuthorizedAsync(
                       client,
                       HttpMethod.Get,
                       "/local/api/v1/admin/accounts",
                       administratorToken))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, oldSession.StatusCode);
            }

            await ExecuteSqlAsync(
                "UPDATE scifi_administrator_sessions SET expires_at_utc = NOW() - INTERVAL '1 minute' " +
                "WHERE token_hash = @tokenHash",
                new NpgsqlParameter("tokenHash", HashSecret(renewedToken)));
            using (var expiredSession = await SendAuthorizedAsync(
                       client,
                       HttpMethod.Get,
                       "/local/api/v1/admin/accounts",
                       renewedToken))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, expiredSession.StatusCode);
                await AssertSirenError(expiredSession, "errors.unauthorized");
            }

            using var recoverySignIn = await PostJsonAsync(client, "/local/api/v1/admin/session", new
            {
                userName = "local-admin",
                password = administratorPassword
            });
            var recoveryTargetToken = AssertBearerToken(recoverySignIn);
            var expiredRecoveryCredential = await authenticationService.IssueRecoveryCredentialAsync();
            await ExecuteSqlAsync(
                "UPDATE scifi_administrator_grants SET expires_at_utc = NOW() - INTERVAL '1 minute' WHERE id = 1");
            var expiredRecovery = await authenticationService.RecoverAsync(
                expiredRecoveryCredential,
                "local-admin",
                "replacement-password-1");
            Assert.Equal(AdministratorOperationResult.InvalidGrant, expiredRecovery);

            var recoveryCredential = await authenticationService.IssueRecoveryCredentialAsync();
            var recoveryResult = await authenticationService.RecoverAsync(
                recoveryCredential,
                "local-admin",
                "replacement-password-1");
            Assert.Equal(AdministratorOperationResult.Succeeded, recoveryResult);

            using (var revokedSession = await SendAuthorizedAsync(
                       client,
                       HttpMethod.Get,
                       "/local/api/v1/admin/accounts",
                       recoveryTargetToken))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, revokedSession.StatusCode);
            }

            using (var oldPasswordSignIn = await PostJsonAsync(client, "/local/api/v1/admin/session", new
                   {
                       userName = "local-admin",
                       password = administratorPassword
                   }))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordSignIn.StatusCode);
            }

            using var newPasswordSignIn = await PostJsonAsync(client, "/local/api/v1/admin/session", new
            {
                userName = "local-admin",
                password = "replacement-password-1"
            });
            Assert.Equal(HttpStatusCode.OK, newPasswordSignIn.StatusCode);
            var logoutToken = AssertBearerToken(newPasswordSignIn);
            using (var logout = await SendAuthorizedAsync(
                       client,
                       HttpMethod.Delete,
                       "/local/api/v1/admin/session",
                       logoutToken))
            {
                Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
            }

            using (var loggedOutRequest = await SendAuthorizedAsync(
                       client,
                       HttpMethod.Get,
                       "/local/api/v1/admin/accounts",
                       logoutToken))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, loggedOutRequest.StatusCode);
            }

            var replayedRecovery = await authenticationService.RecoverAsync(
                recoveryCredential,
                "local-admin",
                "another-replacement-password-1");
            Assert.Equal(AdministratorOperationResult.InvalidGrant, replayedRecovery);
            var leakedEventCount = await ReadScalarAsync<long>(
                "SELECT COUNT(*) FROM public.mt_events WHERE data::text LIKE '%' || @bootstrap || '%' " +
                "OR data::text LIKE '%' || @password || '%' OR data::text LIKE '%' || @token || '%' " +
                "OR data::text LIKE '%' || @recovery || '%'",
                new NpgsqlParameter("bootstrap", bootstrapCredential),
                new NpgsqlParameter("password", administratorPassword),
                new NpgsqlParameter("token", administratorToken),
                new NpgsqlParameter("recovery", recoveryCredential));
            Assert.Equal(0, leakedEventCount);
            Assert.DoesNotContain(
                factory.LogMessages.Messages,
                message => message.Contains(recoveryCredential, StringComparison.Ordinal) ||
                           message.Contains("replacement-password-1", StringComparison.Ordinal));
        }
        finally
        {
            await ClearAuthenticationStoreAsync();
        }
    }

    private async Task PrepareEmptyAuthenticationStoreAsync(IAdministratorAuthenticationService service)
    {
        try
        {
            await service.IssueBootstrapCredentialAsync();
        }
        catch (InvalidOperationException)
        {
            // Existing test accounts are removed immediately below.
        }

        await ClearAuthenticationStoreAsync();
    }

    private async Task ClearAuthenticationStoreAsync() =>
        await ExecuteSqlAsync(
            "DELETE FROM scifi_administrator_audit_events; " +
            "DELETE FROM scifi_administrator_sessions; " +
            "DELETE FROM scifi_administrator_grants; " +
            "DELETE FROM scifi_administrator_accounts");

    private async Task ExecuteSqlAsync(string sql, params NpgsqlParameter[] parameters)
    {
        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<T> ReadScalarAsync<T>(string sql, params NpgsqlParameter[] parameters)
    {
        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        return (T)(await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("Expected a database value."));
    }

    private static async Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string path, object payload) =>
        await client.PostAsync(path, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

    private static async Task<HttpResponseMessage> SendAuthorizedJsonAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        string token,
        object payload)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd("application/vnd.siren+json");
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        string token)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd("application/vnd.siren+json");
        return await client.SendAsync(request);
    }

    private static string AssertBearerToken(HttpResponseMessage response)
    {
        var rawHeader = response.Headers.TryGetValues("Authorization", out var values)
            ? values.SingleOrDefault()
            : null;
        Assert.True(AuthenticationHeaderValue.TryParse(rawHeader, out var authorization));
        Assert.Equal("Bearer", authorization.Scheme);
        Assert.False(string.IsNullOrWhiteSpace(authorization.Parameter));
        return authorization.Parameter!;
    }

    private static async Task AssertSirenError(HttpResponseMessage response, string expectedCode)
    {
        Assert.Equal("application/vnd.siren+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedCode, body.RootElement.GetProperty("properties").GetProperty("code").GetString());
    }

    private static string HashSecret(string secret) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
}

public sealed class AdministratorAuthenticationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _schemaName = $"administrator_tests_{Guid.NewGuid():N}";

    private static string BaseConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__EventStore") ??
        "Host=localhost;Port=5432;Database=scifilarnetwork_tests;Username=postgres;Password=postgres";

    public string ConnectionString { get; private set; } = string.Empty;

    public CapturingLoggerProvider LogMessages { get; } = new();

    public async Task InitializeAsync()
    {
        await using var connection = new NpgsqlConnection(BaseConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE SCHEMA \"{_schemaName}\"", connection);
        await command.ExecuteNonQueryAsync();
        ConnectionString = new NpgsqlConnectionStringBuilder(BaseConnectionString)
        {
            SearchPath = _schemaName
        }.ConnectionString;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await using var connection = new NpgsqlConnection(BaseConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{_schemaName}\" CASCADE", connection);
        await command.ExecuteNonQueryAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("PathBase", "/local");
        builder.UseSetting("ConnectionStrings:EventStore", ConnectionString);
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddProvider(LogMessages);
        });
        builder.ConfigureTestServices(services => services.AddSingleton<IStartupFilter, LoopbackAddressStartupFilter>());
    }
}

public sealed class CapturingLoggerProvider : ILoggerProvider
{
    public ConcurrentBag<string> Messages { get; } = [];

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(ConcurrentBag<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            messages.Add(formatter(state, exception));
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
