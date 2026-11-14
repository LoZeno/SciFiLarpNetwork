using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JasperFx;
using JasperFx.Events;
using Marten;
using Marten.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Api;

[Collection("Backend database integration")]
public sealed class AdministratorAuthenticationTests : IAsyncLifetime
{
    private readonly AdministratorAuthenticationFactory _factory = new();

    public async ValueTask InitializeAsync()
    {
        await _factory.InitializeAsync();
        var store = _factory.Services.GetRequiredService<IDocumentStore>();
        await store.Advanced.ResetAllData();
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task BootstrapCreatesFirstAdministratorAccount()
    {
        var service = _factory.Services.GetRequiredService<IAdministratorProvisioningService>();
        var credential = await service.IssueBootstrapCredentialAsync(TestContext.Current.CancellationToken);
        await _factory.WaitForProjectionsAsync();

        var result = await service.BootstrapAsync(credential, "local-admin", "local-admin-password-1", TestContext.Current.CancellationToken);

        Assert.Equal(AdministratorOperationResult.Succeeded, result);
    }

    [Fact]
    public async Task BootstrapRejectsShortPassword()
    {
        var service = _factory.Services.GetRequiredService<IAdministratorProvisioningService>();
        var credential = await service.IssueBootstrapCredentialAsync(TestContext.Current.CancellationToken);
        await _factory.WaitForProjectionsAsync();

        var result = await service.BootstrapAsync(credential, "local-admin", "short", TestContext.Current.CancellationToken);

        Assert.Equal(AdministratorOperationResult.InvalidGrant, result);
    }

    [Fact]
    public async Task BootstrapCredentialIsStoredAsHash()
    {
        var service = _factory.Services.GetRequiredService<IAdministratorProvisioningService>();
        var credential = await service.IssueBootstrapCredentialAsync(TestContext.Current.CancellationToken);

        await _factory.WaitForProjectionsAsync();

        var grant = await _factory.LoadGrantDocumentAsync();

        Assert.Equal(
            new { IsStored = true, IsPlaintext = false },
            new { IsStored = grant is not null, IsPlaintext = grant?.TokenHash == credential });
    }

    [Fact]
    public async Task ExpiredBootstrapCredentialIsRejected()
    {
        var service = _factory.Services.GetRequiredService<IAdministratorProvisioningService>();
        var credential = await service.IssueBootstrapCredentialAsync(TestContext.Current.CancellationToken);

        await _factory.WaitForProjectionsAsync();
        _factory.ExpireCurrentGrant();

        var result = await service.BootstrapAsync(credential, "local-admin", "local-admin-password-1", TestContext.Current.CancellationToken);

        Assert.Equal(AdministratorOperationResult.InvalidGrant, result);
    }

    [Fact]
    public async Task ConcurrentBootstrapConsumesCredentialExactlyOnce()
    {
        var service = _factory.Services.GetRequiredService<IAdministratorProvisioningService>();
        var credential = await service.IssueBootstrapCredentialAsync(TestContext.Current.CancellationToken);
        await _factory.WaitForProjectionsAsync();

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
        using var client = _factory.CreateClient();
        await _factory.BootstrapAdministratorAsync();

        using var response = await client.PostAsync("/local/api/v1/admin/session",
            new StringContent(JsonSerializer.Serialize(new { userName = "local-admin", password = "local-admin-password-1" }),
                Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        Assert.Equal(
            new { Status = HttpStatusCode.OK, HasBearerToken = true },
            new
            {
                Status = response.StatusCode,
                HasBearerToken = response.Headers.TryGetValues("Authorization", out var values) &&
                                 values.Single().StartsWith("Bearer ", StringComparison.Ordinal)
            });
    }

    [Fact]
    public async Task SignInWithWrongPasswordReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        await _factory.BootstrapAdministratorAsync();

        using var response = await client.PostAsync("/local/api/v1/admin/session",
            new StringContent(JsonSerializer.Serialize(new { userName = "local-admin", password = "wrong-password" }),
                Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SessionTokenIsStoredAsHash()
    {
        using var client = _factory.CreateClient();
        await _factory.BootstrapAdministratorAsync();

        using var response = await client.PostAsync("/local/api/v1/admin/session",
            new StringContent(JsonSerializer.Serialize(new { userName = "local-admin", password = "local-admin-password-1" }),
                Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var token = AuthenticationHeaderValue.Parse(response.Headers.GetValues("Authorization").Single()).Parameter!;

        await _factory.WaitForProjectionsAsync();

        var sessions = await _factory.LoadSessionDocumentsAsync();

        Assert.Equal(
            new { SessionCount = 1, IsPlaintext = false },
            new { SessionCount = sessions.Count, IsPlaintext = sessions.Any(s => s.TokenHash == token) });
    }

    [Fact]
    public async Task RecoverySucceedsWithValidCredential()
    {
        await _factory.BootstrapAdministratorAsync();
        var service = _factory.Services.GetRequiredService<IAdministratorProvisioningService>();
        var credential = await service.IssueRecoveryCredentialAsync(TestContext.Current.CancellationToken);
        await _factory.WaitForProjectionsAsync();

        var result = await service.RecoverAsync(credential, "local-admin", "replacement-password-1", TestContext.Current.CancellationToken);

        Assert.Equal(AdministratorOperationResult.Succeeded, result);
    }

    [Fact]
    public async Task ExpiredRecoveryCredentialIsRejected()
    {
        await _factory.BootstrapAdministratorAsync();
        var service = _factory.Services.GetRequiredService<IAdministratorProvisioningService>();
        var credential = await service.IssueRecoveryCredentialAsync(TestContext.Current.CancellationToken);

        await _factory.WaitForProjectionsAsync();
        _factory.ExpireCurrentGrant();

        var result = await service.RecoverAsync(credential, "local-admin", "replacement-password-1", TestContext.Current.CancellationToken);

        Assert.Equal(AdministratorOperationResult.InvalidGrant, result);
    }

    [Fact]
    public async Task RecoveryRevokesExistingSessions()
    {
        using var client = _factory.CreateClient();
        await _factory.BootstrapAdministratorAsync();
        var token = await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");
        await _factory.RecoverAdministratorAsync();

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, "/local/api/v1/admin/accounts", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RecoveryReplacesPassword()
    {
        using var client = _factory.CreateClient();
        await _factory.BootstrapAdministratorAsync();
        await _factory.RecoverAdministratorAsync();

        using var response = await client.PostAsync("/local/api/v1/admin/session",
            new StringContent(JsonSerializer.Serialize(new { userName = "local-admin", password = "replacement-password-1" }),
                Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RecoveryCredentialCannotBeReplayed()
    {
        await _factory.BootstrapAdministratorAsync();
        var credential = await _factory.RecoverAdministratorAsync();
        var service = _factory.Services.GetRequiredService<IAdministratorProvisioningService>();

        var result = await service.RecoverAsync(credential, "local-admin", "another-replacement-password-1", TestContext.Current.CancellationToken);

        Assert.Equal(AdministratorOperationResult.InvalidGrant, result);
    }

    [Fact]
    public async Task LogoutRevokesSession()
    {
        using var client = _factory.CreateClient();
        await _factory.BootstrapAdministratorAsync();
        await _factory.RecoverAdministratorAsync();
        var token = await AdministratorAuthenticationFactory.SignInAsync(client, "replacement-password-1");

        using var logout = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Delete, "/local/api/v1/admin/session", token);
        logout.EnsureSuccessStatusCode();

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, "/local/api/v1/admin/accounts", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RenewalReturnsNewToken()
    {
        using var client = _factory.CreateClient();
        await _factory.BootstrapAdministratorAsync();
        var token = await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Post, "/local/api/v1/admin/session/renew", token);
        response.EnsureSuccessStatusCode();
        var newToken = AuthenticationHeaderValue.Parse(response.Headers.GetValues("Authorization").Single()).Parameter!;

        Assert.NotEqual(token, newToken);
    }

    [Fact]
    public async Task RenewalRevokesPreviousSession()
    {
        using var client = _factory.CreateClient();
        await _factory.BootstrapAdministratorAsync();
        var token = await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");
        await AdministratorAuthenticationFactory.RenewSessionAsync(client, token);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, "/local/api/v1/admin/accounts", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReaderCanViewAccounts()
    {
        using var client = _factory.CreateClient();
        await _factory.CreateReaderAccountAsync(client);
        var readerToken = await AdministratorAuthenticationFactory.SignInReaderAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, "/local/api/v1/admin/accounts", readerToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReaderCannotCreateAccounts()
    {
        using var client = _factory.CreateClient();
        await _factory.CreateReaderAccountAsync(client);
        var readerToken = await AdministratorAuthenticationFactory.SignInReaderAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, "/local/api/v1/admin/accounts", readerToken,
            new { userName = "denied-user", password = "denied-user-password", role = AdministratorRoles.Reader });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReaderCannotChangeRoles()
    {
        using var client = _factory.CreateClient();
        var readerId = await _factory.CreateReaderAccountAsync(client);
        var readerToken = await AdministratorAuthenticationFactory.SignInReaderAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Put,
            $"/local/api/v1/admin/accounts/{readerId:D}/role", readerToken, new { role = AdministratorRoles.Administrator });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task LastAdministratorCannotBeDemoted()
    {
        using var client = _factory.CreateClient();
        await _factory.BootstrapAdministratorAsync();
        var token = await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");

        await _factory.WaitForProjectionsAsync();
        var accounts = await _factory.LoadAccountDocumentsAsync();
        var adminId = accounts[0].Id;

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Put,
            $"/local/api/v1/admin/accounts/{adminId:D}/role", token, new { role = AdministratorRoles.Reader });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousRequestsAreUnauthorized()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/local/api/v1/admin/accounts", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RemoteAuthenticationRequiresHttps()
    {
        using var client = _factory.CreateClient();
        await _factory.BootstrapAdministratorAsync();
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
        await _factory.BootstrapAdministratorAsync();
        await _factory.WaitForProjectionsAsync();

        var accounts = await _factory.LoadAccountDocumentsAsync();

        Assert.Equal(
            new { AccountCount = 1, ContainsPlaintextPassword = false },
            new
            {
                AccountCount = accounts.Count,
                ContainsPlaintextPassword = accounts.Any(a => a.PasswordHash.Contains("local-admin-password-1", StringComparison.Ordinal))
            });
    }

    [Fact]
    public async Task AuditEventRecordsActor()
    {
        using var client = _factory.CreateClient();
        var (token, readerId) = await _factory.PrepareReaderWithTokenAsync(client);

        await _factory.WaitForProjectionsAsync();
        var adminAccounts = await _factory.LoadAccountDocumentsAsync();
        var adminId = adminAccounts.FirstOrDefault(a => a.Role == AdministratorRoles.Administrator)?.Id
            ?? throw new InvalidOperationException("No admin account found");

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Put,
            $"/local/api/v1/admin/accounts/{readerId:D}/role", token, new { role = AdministratorRoles.Administrator });
        response.EnsureSuccessStatusCode();

        await _factory.WaitForProjectionsAsync();
        var audits = await _factory.LoadAuditDocumentsAsync();
        var roleChangeAudit = audits.FirstOrDefault(a => a.TargetAccountId == readerId && a.EventType == "AdministratorRoleChanged");

        Assert.Equal(adminId, roleChangeAudit?.ActorAccountId);
    }

    [Fact]
    public async Task RoleChangeEventIsAttributedToAuthenticatedAdministrator()
    {
        using var client = _factory.CreateClient();
        var (token, readerId) = await _factory.PrepareReaderWithTokenAsync(client);
        var adminAccounts = await _factory.LoadAccountDocumentsAsync();
        var adminId = adminAccounts.Single(a => a.Role == AdministratorRoles.Administrator).Id;

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Put,
            $"/local/api/v1/admin/accounts/{readerId:D}/role", token, new { role = AdministratorRoles.Administrator });
        response.EnsureSuccessStatusCode();

        var readerEvents = await _factory.LoadStreamEventsAsync(readerId);

        Assert.Equal(adminId.ToString("D"), readerEvents[^1].UserName);
    }

    [Fact]
    public async Task StoreRejectsSecondAccountWithSameNormalizedUserName()
    {
        await _factory.BootstrapAdministratorAsync();
        var existing = (await _factory.LoadAccountDocumentsAsync()).Single();

        var exception = await Record.ExceptionAsync(() => _factory.AppendAccountCreatedAsync(existing.NormalizedUserName));

        Assert.IsType<DocumentAlreadyExistsException>(exception);
    }

    [Fact]
    public async Task CurrentSessionReturnsSignedInAccountName()
    {
        using var client = _factory.CreateClient();
        await _factory.BootstrapAdministratorAsync();
        var token = await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, "/local/api/v1/admin/session", token);

        Assert.Equal(
            new { Status = HttpStatusCode.OK, UserName = "local-admin" },
            new { Status = response.StatusCode, UserName = await ReadPropertyAsync(response, "userName") });
    }

    [Fact]
    public async Task RemoteSignInWithoutHttpsIsForbidden()
    {
        using var client = _factory.CreateClient();
        await _factory.BootstrapAdministratorAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/local/api/v1/admin/session")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { userName = "local-admin", password = "local-admin-password-1" }),
                Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Test-Remote", "true");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SignInWithUnknownAccountReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        await _factory.BootstrapAdministratorAsync();

        using var response = await client.PostAsync("/local/api/v1/admin/session",
            new StringContent(JsonSerializer.Serialize(new { userName = "unknown-admin", password = "local-admin-password-1" }),
                Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SignInWithLegacyPasswordHashUpgradesTheStoredHash()
    {
        using var client = _factory.CreateClient();
        var legacyHash = new PasswordHasher<object>(Options.Create(new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2
        })).HashPassword(new object(), "legacy-password-123");
        await _factory.AppendAccountCreatedAsync("LEGACY-ADMIN", "legacy-admin", legacyHash, AdministratorRoles.Administrator);

        await AdministratorAuthenticationFactory.SignInAsync(client, "legacy-admin", "legacy-password-123");

        var account = (await _factory.LoadAccountDocumentsAsync()).Single();
        Assert.NotEqual(legacyHash, account.PasswordHash);
    }

    [Fact]
    public async Task ExpiredSessionIsRevokedWhenItIsPresented()
    {
        using var client = _factory.CreateClient();
        await _factory.BootstrapAdministratorAsync();
        var token = await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");
        _factory.Clock.Advance(TimeSpan.FromHours(9));

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, "/local/api/v1/admin/accounts", token);

        Assert.Empty(await _factory.LoadSessionDocumentsAsync());
    }

    [Fact]
    public async Task CreatingAccountWithUsedNameReturnsConflict()
    {
        using var client = _factory.CreateClient();
        var (token, _) = await _factory.PrepareReaderWithTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, "/local/api/v1/admin/accounts", token,
            new { userName = "Auditor", password = "another-reader-password-1", role = AdministratorRoles.Reader });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreatingAccountWithInvalidNameReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        await _factory.BootstrapAdministratorAsync();
        var token = await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, "/local/api/v1/admin/accounts", token,
            new { userName = "-invalid", password = "valid-length-password", role = AdministratorRoles.Reader });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AccountCanBeReadById()
    {
        using var client = _factory.CreateClient();
        var (token, readerId) = await _factory.PrepareReaderWithTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, $"/local/api/v1/admin/accounts/{readerId:D}", token);

        Assert.Equal(
            new { Status = HttpStatusCode.OK, UserName = "auditor" },
            new { Status = response.StatusCode, UserName = await ReadPropertyAsync(response, "userName") });
    }

    [Fact]
    public async Task ChangingRoleOfUnknownAccountReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        await _factory.BootstrapAdministratorAsync();
        var token = await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Put,
            $"/local/api/v1/admin/accounts/{Guid.NewGuid():D}/role", token, new { role = AdministratorRoles.Reader });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangingRoleToTheCurrentRoleSucceeds()
    {
        using var client = _factory.CreateClient();
        var (token, readerId) = await _factory.PrepareReaderWithTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Put,
            $"/local/api/v1/admin/accounts/{readerId:D}/role", token, new { role = AdministratorRoles.Reader });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VersionRootAdvertisesAccountsNavigationToAdministrators()
    {
        using var client = _factory.CreateClient();
        await _factory.BootstrapAdministratorAsync();
        var token = await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, "/local/api/v1", token);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Contains("/local/api/v1/admin/accounts", body.RootElement.GetProperty("links").EnumerateArray()
            .Select(link => link.GetProperty("href").GetString()));
    }

    [Fact]
    public async Task BootstrapIsRejectedWhenAnAccountAlreadyExists()
    {
        var service = _factory.Services.GetRequiredService<IAdministratorProvisioningService>();
        var credential = await service.IssueBootstrapCredentialAsync(TestContext.Current.CancellationToken);
        await _factory.AppendAccountCreatedAsync("EXISTING-ADMIN", "existing-admin", "not-a-real-hash", AdministratorRoles.Administrator);

        var result = await service.BootstrapAsync(credential, "local-admin", "local-admin-password-1", TestContext.Current.CancellationToken);

        Assert.Equal(AdministratorOperationResult.AlreadyInitialized, result);
    }

    [Fact]
    public async Task BootstrapRejectsNonHexadecimalCredential()
    {
        var service = _factory.Services.GetRequiredService<IAdministratorProvisioningService>();
        await service.IssueBootstrapCredentialAsync(TestContext.Current.CancellationToken);

        var result = await service.BootstrapAsync("not-a-hex-credential", "local-admin", "local-admin-password-1", TestContext.Current.CancellationToken);

        Assert.Equal(AdministratorOperationResult.InvalidGrant, result);
    }

    [Fact]
    public async Task BootstrapRejectsBlankUserName()
    {
        var service = _factory.Services.GetRequiredService<IAdministratorProvisioningService>();
        var credential = await service.IssueBootstrapCredentialAsync(TestContext.Current.CancellationToken);

        var result = await service.BootstrapAsync(credential, "   ", "local-admin-password-1", TestContext.Current.CancellationToken);

        Assert.Equal(AdministratorOperationResult.InvalidGrant, result);
    }

    [Fact]
    public async Task RecoveryRejectsShortPassword()
    {
        await _factory.BootstrapAdministratorAsync();
        var service = _factory.Services.GetRequiredService<IAdministratorProvisioningService>();
        var credential = await service.IssueRecoveryCredentialAsync(TestContext.Current.CancellationToken);

        var result = await service.RecoverAsync(credential, "local-admin", "short", TestContext.Current.CancellationToken);

        Assert.Equal(AdministratorOperationResult.InvalidGrant, result);
    }

    [Fact]
    public async Task RecoveryOfUnknownAccountReturnsAccountNotFound()
    {
        await _factory.BootstrapAdministratorAsync();
        var service = _factory.Services.GetRequiredService<IAdministratorProvisioningService>();
        var credential = await service.IssueRecoveryCredentialAsync(TestContext.Current.CancellationToken);

        var result = await service.RecoverAsync(credential, "unknown-admin", "replacement-password-1", TestContext.Current.CancellationToken);

        Assert.Equal(AdministratorOperationResult.AccountNotFound, result);
    }

    [Fact]
    public async Task RecoveryCredentialCannotBeIssuedWithoutAccounts()
    {
        var service = _factory.Services.GetRequiredService<IAdministratorProvisioningService>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.IssueRecoveryCredentialAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReaderCannotCreateAccountsThroughTheService()
    {
        using var client = _factory.CreateClient();
        var readerId = await _factory.CreateReaderAccountAsync(client);
        var service = _factory.Services.GetRequiredService<IAdministratorAccountService>();

        var result = await service.CreateAccountAsync(readerId, "new-reader", "new-reader-password-1", AdministratorRoles.Reader, TestContext.Current.CancellationToken);

        Assert.Equal(AdministratorOperationResult.ActorNoLongerAuthorized, result.Result);
    }

    [Fact]
    public async Task ReaderCannotChangeRolesThroughTheService()
    {
        using var client = _factory.CreateClient();
        var readerId = await _factory.CreateReaderAccountAsync(client);
        var service = _factory.Services.GetRequiredService<IAdministratorAccountService>();

        var result = await service.ChangeRoleAsync(readerId, readerId, AdministratorRoles.Administrator, TestContext.Current.CancellationToken);

        Assert.Equal(AdministratorOperationResult.ActorNoLongerAuthorized, result);
    }

    [Fact]
    public async Task ChangingToAnUnknownRoleIsRejected()
    {
        await _factory.BootstrapAdministratorAsync();
        var adminId = (await _factory.LoadAccountDocumentsAsync()).Single().Id;
        var service = _factory.Services.GetRequiredService<IAdministratorAccountService>();

        var result = await service.ChangeRoleAsync(adminId, adminId, "superuser", TestContext.Current.CancellationToken);

        Assert.Equal(AdministratorOperationResult.InvalidGrant, result);
    }

    private static async Task<string> ReadPropertyAsync(HttpResponseMessage response, string name)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return body.RootElement.GetProperty("properties").GetProperty(name).GetString() ?? string.Empty;
    }
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
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IStartupFilter, LoopbackAddressStartupFilter>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    public AdjustableTimeProvider Clock { get; } = new();

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

    public async Task<IReadOnlyList<IEvent>> LoadStreamEventsAsync(Guid streamId)
    {
        await using var session = Services.GetRequiredService<IDocumentStore>().QuerySession();
        return await session.Events.FetchStreamAsync(streamId);
    }

    public Task AppendAccountCreatedAsync(string normalizedUserName) =>
        AppendAccountCreatedAsync(normalizedUserName, normalizedUserName, "not-a-real-hash", AdministratorRoles.Reader);

    public async Task AppendAccountCreatedAsync(string normalizedUserName, string userName, string passwordHash, string role)
    {
        var accountId = Guid.NewGuid();
        await using var session = Services.GetRequiredService<IDocumentStore>().LightweightSession();
        session.Events.StartStream<AdministratorAccountDocument>(
            accountId,
            new AccountCreated(accountId, userName, normalizedUserName, passwordHash, role, DateTimeOffset.UtcNow));
        await session.SaveChangesAsync();
    }

    // Events are immutable history, so expiry is simulated by moving the application clock
    // past the 15-minute credential lifetime instead of rewriting stored events.
    public void ExpireCurrentGrant() => Clock.Advance(TimeSpan.FromMinutes(16));

    public async Task BootstrapAdministratorAsync()
    {
        var service = Services.GetRequiredService<IAdministratorProvisioningService>();
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
        var service = Services.GetRequiredService<IAdministratorProvisioningService>();
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

    public static Task<string> SignInAsync(HttpClient client, string password) =>
        SignInAsync(client, "local-admin", password);

    public static async Task<string> SignInAsync(HttpClient client, string userName, string password)
    {
        using var response = await client.PostAsync("/local/api/v1/admin/session",
            new StringContent(JsonSerializer.Serialize(new { userName, password }),
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
        var readerId = await CreateReaderAccountInternalAsync(client, token);
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
        HttpClient client, HttpMethod method, string path, string token, string? language = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd("application/vnd.siren+json");
        if (language is not null)
        {
            request.Headers.AcceptLanguage.ParseAdd(language);
        }

        return await client.SendAsync(request);
    }
}

public sealed class AdjustableTimeProvider : TimeProvider
{
    private TimeSpan _offset;

    public override DateTimeOffset GetUtcNow() => base.GetUtcNow() + _offset;

    public void Advance(TimeSpan amount) => _offset += amount;
}

public sealed class CapturingLoggerProvider : ILoggerProvider
{
    public ConcurrentBag<string> Messages { get; } = [];

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);

    public void Dispose() => GC.SuppressFinalize(this);

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
