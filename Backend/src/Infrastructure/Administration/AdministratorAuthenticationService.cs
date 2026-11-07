using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

    private static Guid GrantsStreamId => Guid.Parse("00000000-0000-0000-0000-000000000001");

    public async Task<string> IssueBootstrapCredentialAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await LoadAccountsAsync(cancellationToken);
        if (accounts.Count != 0)
        {
            throw new InvalidOperationException("Administrator bootstrap has already been completed.");
        }

        var credential = CreateRandomToken();
        var nowUtc = DateTime.UtcNow;
        var @event = new DomainEvent(
            Guid.NewGuid(),
            AdministratorEvents.BootstrapCredentialIssued,
            1,
            nowUtc,
            JsonSerializer.SerializeToElement(new
            {
                purpose = "bootstrap",
                tokenHash = HashSecret(credential),
                expiresAtUtc = nowUtc.Add(CredentialLifetime)
            }));

        var stream = await eventStore.ReadStreamAsync(GrantsStreamId, cancellationToken);
        var expectedVersion = stream.Count > 0 ? stream[^1].Sequence : 0;
        await eventStore.AppendAsync(GrantsStreamId, expectedVersion, [@event], cancellationToken: cancellationToken);
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
        var nowUtc = DateTime.UtcNow;
        var @event = new DomainEvent(
            Guid.NewGuid(),
            AdministratorEvents.RecoveryCredentialIssued,
            1,
            nowUtc,
            JsonSerializer.SerializeToElement(new
            {
                purpose = "recovery",
                tokenHash = HashSecret(credential),
                expiresAtUtc = nowUtc.Add(CredentialLifetime)
            }));

        var stream = await eventStore.ReadStreamAsync(GrantsStreamId, cancellationToken);
        var expectedVersion = stream.Count > 0 ? stream[^1].Sequence : 0;
        await eventStore.AppendAsync(GrantsStreamId, expectedVersion, [@event], cancellationToken: cancellationToken);
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

        var grant = await ReadValidGrantAsync("bootstrap", credential, cancellationToken);
        if (grant is null)
        {
            return AdministratorOperationResult.InvalidGrant;
        }

        var accounts = await LoadAccountsAsync(cancellationToken);
        if (accounts.Count != 0)
        {
            return AdministratorOperationResult.AlreadyInitialized;
        }

        var accountId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;
        var normalizedUserName = NormalizeUserName(userName);
        var passwordHash = passwordHasher.HashPassword(_passwordSubject, password);

        var accountEvents = new[]
        {
            new DomainEvent(
                Guid.NewGuid(),
                AdministratorEvents.AccountCreated,
                1,
                nowUtc,
                JsonSerializer.SerializeToElement(new
                {
                    userName = userName.Trim(),
                    normalizedUserName,
                    passwordHash,
                    role = AdministratorRoles.Administrator,
                    createdAtUtc = nowUtc
                }))
        };

        try
        {
            await eventStore.AppendAsync(accountId, 0, accountEvents, cancellationToken: cancellationToken);
        }
        catch (StreamConcurrencyException)
        {
            return AdministratorOperationResult.AccountNameAlreadyUsed;
        }

        var auditId = Guid.NewGuid();
        var auditEvents = new[]
        {
            new DomainEvent(
                Guid.NewGuid(),
                AdministratorEvents.AccountCreated,
                1,
                nowUtc,
                JsonSerializer.SerializeToElement(new
                {
                    actorKind = "local-operator",
                    targetAccountId = accountId,
                    eventType = "AdministratorBootstrapped",
                    previousRole = "none",
                    newRole = AdministratorRoles.Administrator,
                    occurredAtUtc = nowUtc
                }))
        };
        await eventStore.AppendAsync(auditId, 0, auditEvents, cancellationToken: cancellationToken);

        var grantStream = await eventStore.ReadStreamAsync(GrantsStreamId, cancellationToken);
        var grantExpectedVersion = grantStream.Count > 0 ? grantStream[^1].Sequence : 0;
        var consumeEvent = new DomainEvent(
            Guid.NewGuid(),
            AdministratorEvents.GrantConsumed,
            1,
            nowUtc,
            JsonSerializer.SerializeToElement(new { purpose = "bootstrap" }));
        await eventStore.AppendAsync(GrantsStreamId, grantExpectedVersion, [consumeEvent], cancellationToken: cancellationToken);

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

        var grant = await ReadValidGrantAsync("recovery", credential, cancellationToken);
        if (grant is null)
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

        var passwordHash = passwordHasher.HashPassword(_passwordSubject, password);
        var nowUtc = DateTime.UtcNow;

        var accountStream = await eventStore.ReadStreamAsync(account.Id, cancellationToken);
        var accountExpectedVersion = accountStream.Count;

        var passwordEvent = new DomainEvent(
            Guid.NewGuid(),
            AdministratorEvents.AccountPasswordChanged,
            1,
            nowUtc,
            JsonSerializer.SerializeToElement(new { passwordHash }));
        await eventStore.AppendAsync(account.Id, accountExpectedVersion, [passwordEvent], cancellationToken: cancellationToken);

        var sessions = await LoadSessionsForAccountAsync(account.Id, cancellationToken);
        foreach (var session in sessions)
        {
            await RevokeSessionByStreamIdAsync(session.Id, cancellationToken);
        }

        var auditId = Guid.NewGuid();
        var auditEvent = new DomainEvent(
            Guid.NewGuid(),
            AdministratorEvents.AccountRoleChanged,
            1,
            nowUtc,
            JsonSerializer.SerializeToElement(new
            {
                actorKind = "local-operator",
                targetAccountId = account.Id,
                eventType = "AdministratorCredentialRecovered",
                previousRole = account.Role,
                newRole = account.Role,
                occurredAtUtc = nowUtc
            }));
        await eventStore.AppendAsync(auditId, 0, [auditEvent], cancellationToken: cancellationToken);

        var grantStream = await eventStore.ReadStreamAsync(GrantsStreamId, cancellationToken);
        var grantExpectedVersion = grantStream.Count > 0 ? grantStream[^1].Sequence : 0;
        var consumeEvent = new DomainEvent(
            Guid.NewGuid(),
            AdministratorEvents.GrantConsumed,
            1,
            nowUtc,
            JsonSerializer.SerializeToElement(new { purpose = "recovery" }));
        await eventStore.AppendAsync(GrantsStreamId, grantExpectedVersion, [consumeEvent], cancellationToken: cancellationToken);

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

        var nowUtc = DateTime.UtcNow;
        string passwordHash = account.PasswordHash;
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            passwordHash = passwordHasher.HashPassword(_passwordSubject, password);
            var accountStream = await eventStore.ReadStreamAsync(account.Id, cancellationToken);
            var accountExpectedVersion = accountStream.Count;
            var passwordEvent = new DomainEvent(
                Guid.NewGuid(),
                AdministratorEvents.AccountPasswordChanged,
                1,
                nowUtc,
                JsonSerializer.SerializeToElement(new { passwordHash }));
            await eventStore.AppendAsync(account.Id, accountExpectedVersion, [passwordEvent], cancellationToken: cancellationToken);
        }

        var token = CreateRandomToken();
        var tokenHash = HashSecret(token);
        var expiresAtUtc = DateTimeOffset.UtcNow.Add(SessionLifetime);

        var sessionStreamId = Guid.NewGuid();
        var sessionEvent = new DomainEvent(
            Guid.NewGuid(),
            AdministratorEvents.SessionCreated,
            1,
            nowUtc,
            JsonSerializer.SerializeToElement(new
            {
                tokenHash,
                accountId = account.Id,
                expiresAtUtc
            }));
        await eventStore.AppendAsync(sessionStreamId, 0, [sessionEvent], cancellationToken: cancellationToken);

        return new AdministratorSession(account.Id, account.UserName, account.Role, expiresAtUtc, token);
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
        var session = await LoadSessionByTokenHashAsync(tokenHash, cancellationToken);
        if (session is null)
        {
            return null;
        }

        if (session.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            await RevokeSessionByStreamIdAsync(session.Id, cancellationToken);
            return null;
        }

        var accounts = await LoadAccountsAsync(cancellationToken);
        var account = accounts.FirstOrDefault(a => a.Id == session.AccountId);
        if (account is null)
        {
            return null;
        }

        return new AdministratorSession(account.Id, account.UserName, account.Role, session.ExpiresAtUtc, string.Empty);
    }

    public async Task<AdministratorSession?> RenewSessionAsync(
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return null;
        }

        var oldTokenHash = HashSecret(sessionToken);
        var oldSession = await LoadSessionByTokenHashAsync(oldTokenHash, cancellationToken);
        if (oldSession is null || oldSession.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        var accounts = await LoadAccountsAsync(cancellationToken);
        var account = accounts.FirstOrDefault(a => a.Id == oldSession.AccountId);
        if (account is null)
        {
            return null;
        }

        await RevokeSessionByStreamIdAsync(oldSession.Id, cancellationToken);

        var newToken = CreateRandomToken();
        var newTokenHash = HashSecret(newToken);
        var expiresAtUtc = DateTimeOffset.UtcNow.Add(SessionLifetime);
        var nowUtc = DateTime.UtcNow;

        var newSessionStreamId = Guid.NewGuid();
        var sessionEvent = new DomainEvent(
            Guid.NewGuid(),
            AdministratorEvents.SessionCreated,
            1,
            nowUtc,
            JsonSerializer.SerializeToElement(new
            {
                tokenHash = newTokenHash,
                accountId = account.Id,
                expiresAtUtc
            }));
        await eventStore.AppendAsync(newSessionStreamId, 0, [sessionEvent], cancellationToken: cancellationToken);

        return new AdministratorSession(account.Id, account.UserName, account.Role, expiresAtUtc, newToken);
    }

    public async Task RevokeSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return;
        }

        var tokenHash = HashSecret(sessionToken);
        var session = await LoadSessionByTokenHashAsync(tokenHash, cancellationToken);
        if (session is not null)
        {
            await RevokeSessionByStreamIdAsync(session.Id, cancellationToken);
        }
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

        if (accounts.Any(a => a.NormalizedUserName == NormalizeUserName(userName)))
        {
            return new AdministratorAccountCreationResult(AdministratorOperationResult.AccountNameAlreadyUsed, null);
        }

        var accountId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;
        var normalizedUserName = NormalizeUserName(userName);
        var passwordHash = passwordHasher.HashPassword(_passwordSubject, password);

        var accountEvents = new[]
        {
            new DomainEvent(
                Guid.NewGuid(),
                AdministratorEvents.AccountCreated,
                1,
                nowUtc,
                JsonSerializer.SerializeToElement(new
                {
                    userName = userName.Trim(),
                    normalizedUserName,
                    passwordHash,
                    role,
                    createdAtUtc = nowUtc
                }),
                actorId: actorAccountId.ToString())
        };

        try
        {
            await eventStore.AppendAsync(accountId, 0, accountEvents, cancellationToken: cancellationToken);
        }
        catch (StreamConcurrencyException)
        {
            return new AdministratorAccountCreationResult(AdministratorOperationResult.AccountNameAlreadyUsed, null);
        }

        var auditId = Guid.NewGuid();
        var auditEvents = new[]
        {
            new DomainEvent(
                Guid.NewGuid(),
                AdministratorEvents.AccountCreated,
                1,
                nowUtc,
                JsonSerializer.SerializeToElement(new
                {
                    actorKind = "administrator",
                    actorId = actorAccountId,
                    targetAccountId = accountId,
                    eventType = "AdministratorAccountCreated",
                    previousRole = "none",
                    newRole = role,
                    occurredAtUtc = nowUtc
                }),
                actorId: actorAccountId.ToString())
        };
        await eventStore.AppendAsync(auditId, 0, auditEvents, cancellationToken: cancellationToken);

        return new AdministratorAccountCreationResult(
            AdministratorOperationResult.Succeeded,
            new AdministratorSummary(accountId, userName.Trim(), role, new DateTimeOffset(nowUtc, TimeSpan.Zero)));
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

        if (targetAccount.Role == role)
        {
            return AdministratorOperationResult.Succeeded;
        }

        if (targetAccount.Role == AdministratorRoles.Administrator && role == AdministratorRoles.Reader &&
            accounts.Count(a => a.Role == AdministratorRoles.Administrator) <= 1)
        {
            return AdministratorOperationResult.LastAdministrator;
        }

        var nowUtc = DateTime.UtcNow;
        var accountStream = await eventStore.ReadStreamAsync(targetAccountId, cancellationToken);
        var accountExpectedVersion = accountStream.Count;

        var roleEvent = new DomainEvent(
            Guid.NewGuid(),
            AdministratorEvents.AccountRoleChanged,
            1,
            nowUtc,
            JsonSerializer.SerializeToElement(new { role }),
            actorId: actorAccountId.ToString());
        await eventStore.AppendAsync(targetAccountId, accountExpectedVersion, [roleEvent], cancellationToken: cancellationToken);

        var auditId = Guid.NewGuid();
        var auditEvent = new DomainEvent(
            Guid.NewGuid(),
            AdministratorEvents.AccountRoleChanged,
            1,
            nowUtc,
            JsonSerializer.SerializeToElement(new
            {
                actorKind = "administrator",
                actorId = actorAccountId,
                targetAccountId,
                eventType = "AdministratorRoleChanged",
                previousRole = targetAccount.Role,
                newRole = role,
                occurredAtUtc = nowUtc
            }),
            actorId: actorAccountId.ToString());
        await eventStore.AppendAsync(auditId, 0, [auditEvent], cancellationToken: cancellationToken);

        return AdministratorOperationResult.Succeeded;
    }

    private async Task<AdministratorAccountDocument?> LoadAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        await using var session = documentStore.QuerySession();
        return await session.LoadAsync<AdministratorAccountDocument>(accountId, cancellationToken);
    }

    private async Task<IReadOnlyList<AdministratorAccountDocument>> LoadAccountsAsync(CancellationToken cancellationToken)
    {
        await using var session = documentStore.QuerySession();
        return await session.Query<AdministratorAccountDocument>().ToListAsync(cancellationToken);
    }

    private async Task<AdministratorSessionDocument?> LoadSessionByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        await using var session = documentStore.QuerySession();
        return await session.Query<AdministratorSessionDocument>()
            .Where(s => s.TokenHash == tokenHash)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<AdministratorSessionDocument>> LoadSessionsForAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        await using var session = documentStore.QuerySession();
        return await session.Query<AdministratorSessionDocument>()
            .Where(s => s.AccountId == accountId)
            .ToListAsync(cancellationToken);
    }

    private async Task RevokeSessionByStreamIdAsync(Guid sessionStreamId, CancellationToken cancellationToken)
    {
        var session = await LoadSessionByStreamIdAsync(sessionStreamId, cancellationToken);
        if (session is null)
        {
            return;
        }

        var nowUtc = DateTime.UtcNow;
        var sessionStream = await eventStore.ReadStreamAsync(sessionStreamId, cancellationToken);
        var sessionExpectedVersion = sessionStream.Count;
        var revokeEvent = new DomainEvent(
            Guid.NewGuid(),
            AdministratorEvents.SessionRevoked,
            1,
            nowUtc,
            JsonSerializer.SerializeToElement(new { reason = "revoke" }));
        await eventStore.AppendAsync(sessionStreamId, sessionExpectedVersion, [revokeEvent], cancellationToken: cancellationToken);
    }

    private async Task<AdministratorSessionDocument?> LoadSessionByStreamIdAsync(Guid sessionStreamId, CancellationToken cancellationToken)
    {
        await using var session = documentStore.QuerySession();
        return await session.LoadAsync<AdministratorSessionDocument>(sessionStreamId, cancellationToken);
    }

    private async Task<CredentialGrant?> ReadValidGrantAsync(string purpose, string credential, CancellationToken cancellationToken)
    {
        var stream = await eventStore.ReadStreamAsync(GrantsStreamId, cancellationToken);
        CredentialGrant? grant = null;

        foreach (var @event in stream)
        {
            if (@event.EventType == AdministratorEvents.BootstrapCredentialIssued ||
                @event.EventType == AdministratorEvents.RecoveryCredentialIssued)
            {
                var payload = @event.Payload;
                grant = new CredentialGrant(
                    payload.GetProperty("purpose").GetString() ?? string.Empty,
                    payload.GetProperty("tokenHash").GetString() ?? string.Empty,
                    payload.GetProperty("expiresAtUtc").GetDateTimeOffset());
            }
            else if (@event.EventType == AdministratorEvents.GrantConsumed)
            {
                var consumedPurpose = @event.Payload.GetProperty("purpose").GetString() ?? string.Empty;
                if (consumedPurpose == purpose && grant is not null && grant.Purpose == purpose)
                {
                    grant = null;
                }
            }
        }

        if (grant is null || grant.Purpose != purpose || grant.ExpiresAtUtc <= DateTimeOffset.UtcNow ||
            string.IsNullOrWhiteSpace(credential))
        {
            return null;
        }

        try
        {
            if (CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(grant.TokenHash),
                    Convert.FromHexString(HashSecret(credential))))
            {
                return grant;
            }

            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static bool IsValidCredentials(string userName, string password) =>
        !string.IsNullOrWhiteSpace(userName) &&
        UserNamePattern.IsMatch(userName.Trim()) &&
        !string.IsNullOrWhiteSpace(password) &&
        password.Length is >= 12 and <= 128;

    private static string NormalizeUserName(string userName) =>
        (userName ?? string.Empty).Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();

    private static string CreateRandomToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string HashSecret(string secret) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    private sealed record CredentialGrant(string Purpose, string TokenHash, DateTimeOffset ExpiresAtUtc);
}
