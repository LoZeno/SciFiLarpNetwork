using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Marten;
using Microsoft.AspNetCore.Identity;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.EventSourcing;
using SciFiLarpNetwork.Backend.Domain.Events;

namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

public sealed class AdministratorAuthenticationService(
    IEventStreamStore eventStore,
    IDocumentStore documentStore,
    IPasswordHasher<object> passwordHasher) : IAdministratorAuthenticationService
{
    private static readonly TimeSpan CredentialLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);
    private static readonly Regex UserNamePattern = new("^[\\p{L}\\p{N}][\\p{L}\\p{N} ._@+-]{2,63}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly object _passwordSubject = new();

    private static Guid GrantsStreamId => new(0x53464C4E, 0x4155, 0x5448, new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 });

    public async Task<string> IssueBootstrapCredentialAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await LoadAccountsAsync(cancellationToken);
        if (accounts.Count != 0)
        {
            throw new InvalidOperationException("Administrator bootstrap has already been completed.");
        }

        var credential = CreateRandomToken();
        var tokenHash = HashSecret(credential);
        var expiresAtUtc = DateTimeOffset.UtcNow.Add(CredentialLifetime);

        var grantStreamId = GrantsStreamId;
        var expectedVersion = await GetCurrentStreamVersionAsync(grantStreamId, cancellationToken);
        await eventStore.AppendAsync(grantStreamId, expectedVersion,
            [new BootstrapCredentialIssued(tokenHash, expiresAtUtc, DateTime.UtcNow)],
            cancellationToken: cancellationToken);

        return credential;
    }

    public async Task<string> IssueRecoveryCredentialAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await LoadAccountsAsync(cancellationToken);
        if (accounts.Count == 0)
        {
            throw new InvalidOperationException("There are no administrator accounts to recover.");
        }

        var credential = CreateRandomToken();
        var tokenHash = HashSecret(credential);
        var expiresAtUtc = DateTimeOffset.UtcNow.Add(CredentialLifetime);

        var grantStreamId = GrantsStreamId;
        var expectedVersion = await GetCurrentStreamVersionAsync(grantStreamId, cancellationToken);
        await eventStore.AppendAsync(grantStreamId, expectedVersion,
            [new RecoveryCredentialIssued(tokenHash, expiresAtUtc, DateTime.UtcNow)],
            cancellationToken: cancellationToken);

        return credential;
    }

    public async Task<AdministratorOperationResult> BootstrapAsync(
        string credential,
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidCredentials(userName, password))
        {
            return AdministratorOperationResult.InvalidGrant;
        }

        var grant = await LoadGrantAsync(cancellationToken);
        if (grant is null || grant.Purpose != "bootstrap" || !IsValidGrantToken(grant, credential))
        {
            return AdministratorOperationResult.InvalidGrant;
        }

        var accounts = await LoadAccountsAsync(cancellationToken);
        if (accounts.Count != 0)
        {
            return AdministratorOperationResult.AlreadyInitialized;
        }

        var accountId = Guid.NewGuid();
        var normalizedUserName = NormalizeUserName(userName);
        var passwordHash = passwordHasher.HashPassword(_passwordSubject, password);
        var occurredAtUtc = DateTime.UtcNow;

        var accountStreamId = accountId;
        try
        {
            await eventStore.AppendAsync(accountStreamId, 0,
                [
                    new AccountCreated(accountId, userName.Trim(), normalizedUserName, passwordHash, AdministratorRoles.Administrator, occurredAtUtc),
                ],
                cancellationToken: cancellationToken);
        }
        catch (StreamConcurrencyException)
        {
            return AdministratorOperationResult.AccountNameAlreadyUsed;
        }

        var grantStreamId = GrantsStreamId;
        var grantVersion = await GetCurrentStreamVersionAsync(grantStreamId, cancellationToken);
        await eventStore.AppendAsync(grantStreamId, grantVersion,
            [new GrantConsumed(accountId, occurredAtUtc)],
            cancellationToken: cancellationToken);

        var auditId = Guid.NewGuid();
        await eventStore.AppendAsync(auditId, 0,
            [
                new AuditEventLogged(auditId, "local-operator", null, accountId, "AdministratorBootstrapped", "none", AdministratorRoles.Administrator, occurredAtUtc),
            ],
            cancellationToken: cancellationToken);

        return AdministratorOperationResult.Succeeded;
    }

    public async Task<AdministratorOperationResult> RecoverAsync(
        string credential,
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidCredentials(userName, password))
        {
            return AdministratorOperationResult.InvalidGrant;
        }

        var grant = await LoadGrantAsync(cancellationToken);
        if (grant is null || grant.Purpose != "recovery" || !IsValidGrantToken(grant, credential))
        {
            return AdministratorOperationResult.InvalidGrant;
        }

        var normalizedUserName = NormalizeUserName(userName);
        var accounts = await LoadAccountsAsync(cancellationToken);
        var account = accounts.FirstOrDefault(a => a.NormalizedUserName == normalizedUserName);
        if (account is null)
        {
            return AdministratorOperationResult.AccountNotFound;
        }

        var accountId = account.Id;
        var passwordHash = passwordHasher.HashPassword(_passwordSubject, password);
        var occurredAtUtc = DateTime.UtcNow;

        var accountStreamId = accountId;
        var accountVersion = await GetCurrentStreamVersionAsync(accountStreamId, cancellationToken);
        await eventStore.AppendAsync(accountStreamId, accountVersion,
            [new AccountPasswordChanged(accountId, passwordHash, occurredAtUtc)],
            cancellationToken: cancellationToken);

        var sessions = await LoadSessionsForAccountAsync(accountId, cancellationToken);
        foreach (var session in sessions)
        {
            var sessionStreamId = session.Id;
            var sessionVersion = await GetCurrentStreamVersionAsync(sessionStreamId, cancellationToken);
            await eventStore.AppendAsync(sessionStreamId, sessionVersion,
                [new SessionRevoked(session.Id, accountId, occurredAtUtc)],
                cancellationToken: cancellationToken);
        }

        var grantStreamId = GrantsStreamId;
        var grantVersion = await GetCurrentStreamVersionAsync(grantStreamId, cancellationToken);
        await eventStore.AppendAsync(grantStreamId, grantVersion,
            [new GrantConsumed(accountId, occurredAtUtc)],
            cancellationToken: cancellationToken);

        var auditId = Guid.NewGuid();
        await eventStore.AppendAsync(auditId, 0,
            [
                new AuditEventLogged(auditId, "local-operator", null, accountId, "AdministratorCredentialRecovered", account.Role, account.Role, occurredAtUtc),
            ],
            cancellationToken: cancellationToken);

        return AdministratorOperationResult.Succeeded;
    }

    public async Task<AdministratorSession?> SignInAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedUserName = NormalizeUserName(userName);
        var accounts = await LoadAccountsAsync(cancellationToken);
        var account = accounts.FirstOrDefault(a => a.NormalizedUserName == normalizedUserName);
        if (account is null)
        {
            return null;
        }

        var verification = passwordHasher.VerifyHashedPassword(_passwordSubject, account.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var accountId = account.Id;
        var storedUserName = account.UserName;
        var role = account.Role;

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var newPasswordHash = passwordHasher.HashPassword(_passwordSubject, password);
            var accountStreamId = accountId;
            var accountVersion = await GetCurrentStreamVersionAsync(accountStreamId, cancellationToken);
            await eventStore.AppendAsync(accountStreamId, accountVersion,
                [new AccountPasswordChanged(accountId, newPasswordHash, DateTime.UtcNow)],
                cancellationToken: cancellationToken);
        }

        var sessionId = Guid.NewGuid();
        var token = CreateRandomToken();
        var tokenHash = HashSecret(token);
        var expiresAtUtc = DateTimeOffset.UtcNow.Add(SessionLifetime);

        var sessionStreamId = sessionId;
        await eventStore.AppendAsync(sessionStreamId, 0,
            [new SessionCreated(sessionId, tokenHash, accountId, expiresAtUtc, DateTime.UtcNow)],
            cancellationToken: cancellationToken);

        return new AdministratorSession(accountId, storedUserName, role, expiresAtUtc, token);
    }

    public async Task<AdministratorSession?> ValidateSessionAsync(
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return null;
        }

        var tokenHash = HashSecret(sessionToken);
        var sessions = await LoadSessionDocumentsAsync(cancellationToken);
        var session = sessions.FirstOrDefault(s => s.TokenHash == tokenHash);
        if (session is null)
        {
            return null;
        }

        var expiresAtUtc = session.ExpiresAtUtc;
        if (expiresAtUtc <= DateTimeOffset.UtcNow)
        {
            var sessionStreamId = session.Id;
            var sessionVersion = await GetCurrentStreamVersionAsync(sessionStreamId, cancellationToken);
            await eventStore.AppendAsync(sessionStreamId, sessionVersion,
                [new SessionRevoked(session.Id, session.AccountId, DateTime.UtcNow)],
                cancellationToken: cancellationToken);
            return null;
        }

        var accounts = await LoadAccountsAsync(cancellationToken);
        var account = accounts.FirstOrDefault(a => a.Id == session.AccountId);
        if (account is null)
        {
            return null;
        }

        return new AdministratorSession(account.Id, account.UserName, account.Role, expiresAtUtc, string.Empty);
    }

    public async Task<AdministratorSession?> RenewSessionAsync(
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return null;
        }

        var tokenHash = HashSecret(sessionToken);
        var sessions = await LoadSessionDocumentsAsync(cancellationToken);
        var oldSession = sessions.FirstOrDefault(s => s.TokenHash == tokenHash);
        if (oldSession is null)
        {
            return null;
        }

        if (oldSession.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        var accountId = oldSession.AccountId;
        var accounts = await LoadAccountsAsync(cancellationToken);
        var account = accounts.FirstOrDefault(a => a.Id == accountId);
        if (account is null)
        {
            return null;
        }

        var occurredAtUtc = DateTime.UtcNow;
        var oldSessionStreamId = oldSession.Id;
        var oldSessionVersion = await GetCurrentStreamVersionAsync(oldSessionStreamId, cancellationToken);
        await eventStore.AppendAsync(oldSessionStreamId, oldSessionVersion,
            [new SessionRevoked(oldSession.Id, accountId, occurredAtUtc)],
            cancellationToken: cancellationToken);

        var newSessionId = Guid.NewGuid();
        var newToken = CreateRandomToken();
        var newTokenHash = HashSecret(newToken);
        var expiresAtUtc = DateTimeOffset.UtcNow.Add(SessionLifetime);

        var newSessionStreamId = newSessionId;
        await eventStore.AppendAsync(newSessionStreamId, 0,
            [new SessionCreated(newSessionId, newTokenHash, accountId, expiresAtUtc, occurredAtUtc)],
            cancellationToken: cancellationToken);

        return new AdministratorSession(accountId, account.UserName, account.Role, expiresAtUtc, newToken);
    }

    public async Task RevokeSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return;
        }

        var tokenHash = HashSecret(sessionToken);
        var sessions = await LoadSessionDocumentsAsync(cancellationToken);
        var session = sessions.FirstOrDefault(s => s.TokenHash == tokenHash);
        if (session is null)
        {
            return;
        }

        var sessionStreamId = session.Id;
        var sessionVersion = await GetCurrentStreamVersionAsync(sessionStreamId, cancellationToken);
        await eventStore.AppendAsync(sessionStreamId, sessionVersion,
            [new SessionRevoked(session.Id, session.AccountId, DateTime.UtcNow)],
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<AdministratorSummary>> GetAdministratorsAsync(
        CancellationToken cancellationToken = default)
    {
        var accounts = await LoadAccountsAsync(cancellationToken);
        return accounts
            .OrderBy(a => a.NormalizedUserName)
            .Select(a => new AdministratorSummary(a.Id, a.UserName, a.Role, a.CreatedAtUtc))
            .ToArray();
    }

    public async Task<AdministratorAccountCreationResult> CreateAccountAsync(
        Guid actorAccountId,
        string userName,
        string password,
        string role,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidCredentials(userName, password) ||
            role is not (AdministratorRoles.Reader or AdministratorRoles.Administrator))
        {
            return new AdministratorAccountCreationResult(AdministratorOperationResult.InvalidGrant, null);
        }

        var accounts = await LoadAccountsAsync(cancellationToken);
        var actorAccount = accounts.FirstOrDefault(a => a.Id == actorAccountId);
        if (actorAccount is null || actorAccount.Role != AdministratorRoles.Administrator)
        {
            return new AdministratorAccountCreationResult(AdministratorOperationResult.ActorNoLongerAuthorized, null);
        }

        var accountId = Guid.NewGuid();
        var createdAtUtc = DateTime.UtcNow;
        var normalizedUserName = NormalizeUserName(userName);
        var passwordHash = passwordHasher.HashPassword(_passwordSubject, password);

        var accountStreamId = accountId;
        try
        {
            await eventStore.AppendAsync(accountStreamId, 0,
                [new AccountCreated(accountId, userName.Trim(), normalizedUserName, passwordHash, role, createdAtUtc)],
                cancellationToken: cancellationToken);
        }
        catch (StreamConcurrencyException)
        {
            return new AdministratorAccountCreationResult(AdministratorOperationResult.AccountNameAlreadyUsed, null);
        }

        var auditId = Guid.NewGuid();
        await eventStore.AppendAsync(auditId, 0,
            [
                new AuditEventLogged(auditId, "administrator", actorAccountId, accountId, "AdministratorAccountCreated", "none", role, createdAtUtc),
            ],
            cancellationToken: cancellationToken);

        return new AdministratorAccountCreationResult(
            AdministratorOperationResult.Succeeded,
            new AdministratorSummary(accountId, userName.Trim(), role, new DateTimeOffset(createdAtUtc, TimeSpan.Zero)));
    }

    public async Task<AdministratorOperationResult> ChangeRoleAsync(
        Guid actorAccountId,
        Guid targetAccountId,
        string role,
        CancellationToken cancellationToken = default)
    {
        if (role is not (AdministratorRoles.Reader or AdministratorRoles.Administrator))
        {
            return AdministratorOperationResult.AccountNotFound;
        }

        var accounts = await LoadAccountsAsync(cancellationToken);
        var actorAccount = accounts.FirstOrDefault(a => a.Id == actorAccountId);
        if (actorAccount is null || actorAccount.Role != AdministratorRoles.Administrator)
        {
            return AdministratorOperationResult.ActorNoLongerAuthorized;
        }

        var targetAccount = accounts.FirstOrDefault(a => a.Id == targetAccountId);
        if (targetAccount is null)
        {
            return AdministratorOperationResult.AccountNotFound;
        }

        var previousRole = targetAccount.Role;
        if (previousRole == role)
        {
            return AdministratorOperationResult.Succeeded;
        }

        if (previousRole == AdministratorRoles.Administrator && role == AdministratorRoles.Reader)
        {
            var adminCount = accounts.Count(a => a.Role == AdministratorRoles.Administrator);
            if (adminCount <= 1)
            {
                return AdministratorOperationResult.LastAdministrator;
            }
        }

        var occurredAtUtc = DateTime.UtcNow;
        var accountStreamId = targetAccountId;
        var accountVersion = await GetCurrentStreamVersionAsync(accountStreamId, cancellationToken);
        await eventStore.AppendAsync(accountStreamId, accountVersion,
            [new AccountRoleChanged(targetAccountId, previousRole, role, actorAccountId, occurredAtUtc)],
            cancellationToken: cancellationToken);

        var auditId = Guid.NewGuid();
        await eventStore.AppendAsync(auditId, 0,
            [
                new AuditEventLogged(auditId, "administrator", actorAccountId, targetAccountId, "AdministratorRoleChanged", previousRole, role, occurredAtUtc),
            ],
            cancellationToken: cancellationToken);

        return AdministratorOperationResult.Succeeded;
    }

    private async Task<long> GetCurrentStreamVersionAsync(Guid streamId, CancellationToken cancellationToken)
    {
        var stream = await eventStore.ReadStreamAsync(streamId, cancellationToken);
        return stream.Count > 0 ? stream[^1].Sequence : 0;
    }

    private async Task<IReadOnlyList<AdministratorAccountDocument>> LoadAccountsAsync(CancellationToken cancellationToken)
    {
        await using var session = documentStore.QuerySession();
        return await session.Query<AdministratorAccountDocument>().ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<AdministratorSessionDocument>> LoadSessionDocumentsAsync(CancellationToken cancellationToken)
    {
        await using var session = documentStore.QuerySession();
        return await session.Query<AdministratorSessionDocument>().ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<AdministratorSessionDocument>> LoadSessionsForAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        await using var session = documentStore.QuerySession();
        return await session.Query<AdministratorSessionDocument>()
            .Where(s => s.AccountId == accountId)
            .ToListAsync(cancellationToken);
    }

    private async Task<AdministratorGrantDocument?> LoadGrantAsync(CancellationToken cancellationToken)
    {
        await using var session = documentStore.QuerySession();
        return await session.LoadAsync<AdministratorGrantDocument>(GrantsStreamId, cancellationToken);
    }

    private static bool IsValidGrantToken(AdministratorGrantDocument grant, string credential)
    {
        if (grant.ExpiresAtUtc <= DateTimeOffset.UtcNow || string.IsNullOrWhiteSpace(credential))
        {
            return false;
        }

        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(grant.TokenHash),
                Convert.FromHexString(HashSecret(credential)));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsValidCredentials(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var trimmed = userName.Trim();
        if (trimmed.Length is < 3 or > 64 || !UserNamePattern.IsMatch(trimmed))
        {
            return false;
        }

        return password.Length is >= 12 and <= 128;
    }

    private static string NormalizeUserName(string userName)
    {
        var trimmed = userName.Trim();
        return System.Text.Encoding.UTF8.GetString(
            System.Text.Encoding.UTF8.GetBytes(trimmed.Normalize(System.Text.NormalizationForm.FormKC))).ToUpperInvariant();
    }

    private static string CreateRandomToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }

    private static string HashSecret(string secret)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(bytes);
    }
}
