using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using JasperFx;
using Marten;
using Microsoft.AspNetCore.Identity;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.EventSourcing;
using SciFiLarpNetwork.Backend.Domain.Events;

namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

/// <summary>
/// Administrator commands follow Marten's "CQRS Command Handler Workflow": each command opens one
/// session, loads the write models it decides on with <c>FetchForWriting</c> (which records the
/// stream version for optimistic concurrency), appends events, and commits everything with a single
/// <c>SaveChangesAsync</c>. The administrator projections run Inline, so queries and
/// <c>FetchForWriting</c> always observe the committed events.
/// </summary>
public sealed class AdministratorAuthenticationService(
    IDocumentStore documentStore,
    IEventActorContext actorContext,
    IPasswordHasher<object> passwordHasher,
    TimeProvider timeProvider) : IAdministratorAuthenticationService
{
    private const string BootstrapPurpose = "bootstrap";
    private const string RecoveryPurpose = "recovery";
    private static readonly TimeSpan CredentialLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);
    private static readonly Regex UserNamePattern = new("^[\\p{L}\\p{N}][\\p{L}\\p{N} ._@+-]{2,63}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly object _passwordSubject = new();

    private static Guid GrantsStreamId => new(0x53464C4E, 0x4155, 0x5448, new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 });

    public Task<string> IssueBootstrapCredentialAsync(CancellationToken cancellationToken = default) =>
        IssueCredentialAsync(
            requireAccounts: false,
            "Administrator bootstrap has already been completed.",
            (tokenHash, expiresAtUtc, now) => new BootstrapCredentialIssued(tokenHash, expiresAtUtc, now),
            cancellationToken);

    public Task<string> IssueRecoveryCredentialAsync(CancellationToken cancellationToken = default) =>
        IssueCredentialAsync(
            requireAccounts: true,
            "There are no administrator accounts to recover.",
            (tokenHash, expiresAtUtc, now) => new RecoveryCredentialIssued(tokenHash, expiresAtUtc, now),
            cancellationToken);

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

        await using var session = OpenSession();
        var grant = await session.Events.FetchForWriting<AdministratorGrantDocument>(GrantsStreamId, cancellationToken);
        if (!IsUsableGrant(grant.Aggregate, BootstrapPurpose, credential))
        {
            return AdministratorOperationResult.InvalidGrant;
        }

        if (await session.Query<AdministratorAccountDocument>().AnyAsync(cancellationToken))
        {
            return AdministratorOperationResult.AlreadyInitialized;
        }

        var accountId = Guid.NewGuid();
        var now = timeProvider.GetUtcNow();
        session.Events.StartStream<AdministratorAccountDocument>(
            accountId,
            new AccountCreated(
                accountId,
                userName.Trim(),
                NormalizeUserName(userName),
                passwordHasher.HashPassword(_passwordSubject, password),
                AdministratorRoles.Administrator,
                now));
        grant.AppendOne(new GrantConsumed(accountId, now));
        StartAuditStream(session, "local-operator", null, accountId, "AdministratorBootstrapped", "none", AdministratorRoles.Administrator, now);

        // A concurrent command that consumed the same grant first makes the grant stream
        // version stale, so this commit is rejected and nothing is written.
        return await TrySaveAsync(session, cancellationToken)
            ? AdministratorOperationResult.Succeeded
            : AdministratorOperationResult.InvalidGrant;
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

        await using var session = OpenSession();
        var grant = await session.Events.FetchForWriting<AdministratorGrantDocument>(GrantsStreamId, cancellationToken);
        if (!IsUsableGrant(grant.Aggregate, RecoveryPurpose, credential))
        {
            return AdministratorOperationResult.InvalidGrant;
        }

        var accountId = await FindAccountIdAsync(session, userName, cancellationToken);
        if (accountId is null)
        {
            return AdministratorOperationResult.AccountNotFound;
        }

        var account = await session.Events.FetchForWriting<AdministratorAccountDocument>(accountId.Value, cancellationToken);
        if (account.Aggregate is not { } current)
        {
            return AdministratorOperationResult.AccountNotFound;
        }

        var now = timeProvider.GetUtcNow();
        account.AppendOne(new AccountPasswordChanged(current.Id, passwordHasher.HashPassword(_passwordSubject, password), now));

        var sessionIds = await session.Query<AdministratorSessionDocument>()
            .Where(s => s.AccountId == current.Id)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
        if (sessionIds.Count != 0)
        {
            var sessionStreams = await session.Events.FetchManyForWriting<AdministratorSessionDocument>(sessionIds.ToArray(), cancellationToken);
            foreach (var sessionStream in sessionStreams)
            {
                if (sessionStream.Aggregate is { } existingSession)
                {
                    sessionStream.AppendOne(new SessionRevoked(existingSession.Id, current.Id, now));
                }
            }
        }

        grant.AppendOne(new GrantConsumed(current.Id, now));
        StartAuditStream(session, "local-operator", null, current.Id, "AdministratorCredentialRecovered", current.Role, current.Role, now);

        return await TrySaveAsync(session, cancellationToken)
            ? AdministratorOperationResult.Succeeded
            : AdministratorOperationResult.InvalidGrant;
    }

    public async Task<AdministratorSession?> SignInAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedUserName = NormalizeUserName(userName);
        AdministratorAccountDocument? account;
        await using (var query = documentStore.QuerySession())
        {
            account = await query.Query<AdministratorAccountDocument>()
                .Where(a => a.NormalizedUserName == normalizedUserName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (account is null)
        {
            return null;
        }

        var verification = passwordHasher.VerifyHashedPassword(_passwordSubject, account.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        // Sign-in is anonymous at the HTTP level; the session events are attributed to the
        // account whose password was just verified, never to a client-supplied identity.
        await using var session = OpenSession(account.Id.ToString("D"));
        var now = timeProvider.GetUtcNow();
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var accountStream = await session.Events.FetchForWriting<AdministratorAccountDocument>(account.Id, cancellationToken);
            accountStream.AppendOne(new AccountPasswordChanged(account.Id, passwordHasher.HashPassword(_passwordSubject, password), now));
        }

        var sessionId = Guid.NewGuid();
        var token = CreateRandomToken();
        var expiresAtUtc = now.Add(SessionLifetime);
        session.Events.StartStream<AdministratorSessionDocument>(
            sessionId,
            new SessionCreated(sessionId, HashSecret(token), account.Id, expiresAtUtc, now));
        await session.SaveChangesAsync(cancellationToken);

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

        AdministratorSessionDocument? existing;
        AdministratorAccountDocument? account;
        await using (var query = documentStore.QuerySession())
        {
            existing = await FindSessionAsync(query, sessionToken, cancellationToken);
            if (existing is null)
            {
                return null;
            }

            account = await query.LoadAsync<AdministratorAccountDocument>(existing.AccountId, cancellationToken);
        }

        if (existing.ExpiresAtUtc <= timeProvider.GetUtcNow())
        {
            await RevokeAsync(existing, cancellationToken);
            return null;
        }

        return account is null
            ? null
            : new AdministratorSession(account.Id, account.UserName, account.Role, existing.ExpiresAtUtc, string.Empty);
    }

    public async Task<AdministratorSession?> RenewSessionAsync(
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return null;
        }

        await using var session = OpenSession();
        var existing = await FindSessionAsync(session, sessionToken, cancellationToken);
        if (existing is null || existing.ExpiresAtUtc <= timeProvider.GetUtcNow())
        {
            return null;
        }

        var account = await session.LoadAsync<AdministratorAccountDocument>(existing.AccountId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var oldSession = await session.Events.FetchForWriting<AdministratorSessionDocument>(existing.Id, cancellationToken);
        if (oldSession.Aggregate is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        oldSession.AppendOne(new SessionRevoked(existing.Id, account.Id, now));

        var newSessionId = Guid.NewGuid();
        var newToken = CreateRandomToken();
        var expiresAtUtc = now.Add(SessionLifetime);
        session.Events.StartStream<AdministratorSessionDocument>(
            newSessionId,
            new SessionCreated(newSessionId, HashSecret(newToken), account.Id, expiresAtUtc, now));

        // A concurrent renewal or logout of the same session wins; this one is rejected.
        return await TrySaveAsync(session, cancellationToken)
            ? new AdministratorSession(account.Id, account.UserName, account.Role, expiresAtUtc, newToken)
            : null;
    }

    public async Task RevokeSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return;
        }

        AdministratorSessionDocument? existing;
        await using (var query = documentStore.QuerySession())
        {
            existing = await FindSessionAsync(query, sessionToken, cancellationToken);
        }

        if (existing is not null)
        {
            await RevokeAsync(existing, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<AdministratorSummary>> GetAdministratorsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var query = documentStore.QuerySession();
        var accounts = await query.Query<AdministratorAccountDocument>().ToListAsync(cancellationToken);
        return accounts
            .OrderBy(a => a.NormalizedUserName, StringComparer.Ordinal)
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

        await using var session = OpenSession();
        var actor = await session.Events.FetchForWriting<AdministratorAccountDocument>(actorAccountId, cancellationToken);
        if (actor.Aggregate is not { Role: AdministratorRoles.Administrator })
        {
            return new AdministratorAccountCreationResult(AdministratorOperationResult.ActorNoLongerAuthorized, null);
        }

        // Guards against the actor being demoted concurrently even though nothing is appended
        // to the actor's own stream (Marten "Enforcing Consistency Without Appending Events").
        actor.AlwaysEnforceConsistency = true;

        var normalizedUserName = NormalizeUserName(userName);
        if (await FindAccountIdAsync(session, userName, cancellationToken) is not null)
        {
            return new AdministratorAccountCreationResult(AdministratorOperationResult.AccountNameAlreadyUsed, null);
        }

        var accountId = Guid.NewGuid();
        var now = timeProvider.GetUtcNow();
        session.Events.StartStream<AdministratorAccountDocument>(
            accountId,
            new AccountCreated(accountId, userName.Trim(), normalizedUserName, passwordHasher.HashPassword(_passwordSubject, password), role, now));
        StartAuditStream(session, "administrator", actorAccountId, accountId, "AdministratorAccountCreated", "none", role, now);

        try
        {
            return await TrySaveAsync(session, cancellationToken)
                ? new AdministratorAccountCreationResult(
                    AdministratorOperationResult.Succeeded,
                    new AdministratorSummary(accountId, userName.Trim(), role, now))
                : new AdministratorAccountCreationResult(AdministratorOperationResult.ActorNoLongerAuthorized, null);
        }
        catch (DocumentAlreadyExistsException)
        {
            // The unique index on NormalizedUserName rejects a concurrent command that
            // claimed the same name after the lookup above.
            return new AdministratorAccountCreationResult(AdministratorOperationResult.AccountNameAlreadyUsed, null);
        }
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

        await using var session = OpenSession();
        var actor = await session.Events.FetchForWriting<AdministratorAccountDocument>(actorAccountId, cancellationToken);
        if (actor.Aggregate is not { Role: AdministratorRoles.Administrator })
        {
            return AdministratorOperationResult.ActorNoLongerAuthorized;
        }

        var target = actorAccountId == targetAccountId
            ? actor
            : await session.Events.FetchForWriting<AdministratorAccountDocument>(targetAccountId, cancellationToken);
        if (target.Aggregate is not { } targetAccount)
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
            var administratorCount = await session.Query<AdministratorAccountDocument>()
                .Where(a => a.Role == AdministratorRoles.Administrator)
                .CountAsync(cancellationToken);
            if (administratorCount <= 1)
            {
                return AdministratorOperationResult.LastAdministrator;
            }
        }

        actor.AlwaysEnforceConsistency = true;
        var now = timeProvider.GetUtcNow();
        target.AppendOne(new AccountRoleChanged(targetAccountId, previousRole, role, actorAccountId, now));
        StartAuditStream(session, "administrator", actorAccountId, targetAccountId, "AdministratorRoleChanged", previousRole, role, now);

        return await TrySaveAsync(session, cancellationToken)
            ? AdministratorOperationResult.Succeeded
            : AdministratorOperationResult.ActorNoLongerAuthorized;
    }

    private async Task<string> IssueCredentialAsync(
        bool requireAccounts,
        string rejectionMessage,
        Func<string, DateTimeOffset, DateTimeOffset, object> createEvent,
        CancellationToken cancellationToken)
    {
        await using var session = OpenSession();
        var hasAccounts = await session.Query<AdministratorAccountDocument>().AnyAsync(cancellationToken);
        if (hasAccounts != requireAccounts)
        {
            throw new InvalidOperationException(rejectionMessage);
        }

        var credential = CreateRandomToken();
        var now = timeProvider.GetUtcNow();
        var grant = await session.Events.FetchForWriting<AdministratorGrantDocument>(GrantsStreamId, cancellationToken);
        grant.AppendOne(createEvent(HashSecret(credential), now.Add(CredentialLifetime), now));
        await session.SaveChangesAsync(cancellationToken);

        return credential;
    }

    private async Task RevokeAsync(AdministratorSessionDocument existing, CancellationToken cancellationToken)
    {
        await using var session = OpenSession(existing.AccountId.ToString("D"));
        var stream = await session.Events.FetchForWriting<AdministratorSessionDocument>(existing.Id, cancellationToken);
        if (stream.Aggregate is null)
        {
            return;
        }

        stream.AppendOne(new SessionRevoked(existing.Id, existing.AccountId, timeProvider.GetUtcNow()));

        // A concurrent revocation of the same session already achieved the same outcome.
        await TrySaveAsync(session, cancellationToken);
    }

    /// <summary>
    /// Opens a lightweight session whose events are attributed through Marten's
    /// <c>LastModifiedBy</c> user-name metadata. HTTP-originated changes use the authenticated
    /// actor; <paramref name="verifiedActorId"/> is used only when the server itself has just
    /// established the actor (sign-in, or revoking an expired session during authentication).
    /// </summary>
    private IDocumentSession OpenSession(string? verifiedActorId = null)
    {
        var actorId = actorContext.ActorId ?? verifiedActorId;
        if (actorContext.RequiresAuthenticatedActor && string.IsNullOrWhiteSpace(actorId))
        {
            throw new UnauthorizedAccessException("An authenticated actor is required to append events from an HTTP request.");
        }

        var session = documentStore.LightweightSession();
        session.LastModifiedBy = actorId;
        return session;
    }

    private static async Task<bool> TrySaveAsync(IDocumentSession session, CancellationToken cancellationToken)
    {
        try
        {
            await session.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (ConcurrencyException)
        {
            return false;
        }
    }

    private static void StartAuditStream(
        IDocumentSession session,
        string actorKind,
        Guid? actorAccountId,
        Guid targetAccountId,
        string eventType,
        string previousRole,
        string newRole,
        DateTimeOffset occurredAtUtc)
    {
        var auditId = Guid.NewGuid();
        session.Events.StartStream<AdministratorAuditDocument>(
            auditId,
            new AuditEventLogged(auditId, actorKind, actorAccountId, targetAccountId, eventType, previousRole, newRole, occurredAtUtc));
    }

    private static async Task<Guid?> FindAccountIdAsync(IQuerySession session, string userName, CancellationToken cancellationToken)
    {
        var normalizedUserName = NormalizeUserName(userName);
        var account = await session.Query<AdministratorAccountDocument>()
            .Where(a => a.NormalizedUserName == normalizedUserName)
            .FirstOrDefaultAsync(cancellationToken);
        return account?.Id;
    }

    private static Task<AdministratorSessionDocument?> FindSessionAsync(
        IQuerySession session,
        string sessionToken,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashSecret(sessionToken);
        return session.Query<AdministratorSessionDocument>()
            .Where(s => s.TokenHash == tokenHash)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private bool IsUsableGrant(AdministratorGrantDocument? grant, string purpose, string credential)
    {
        if (grant is null ||
            grant.Purpose != purpose ||
            grant.ExpiresAtUtc <= timeProvider.GetUtcNow() ||
            string.IsNullOrWhiteSpace(credential))
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
        return Encoding.UTF8.GetString(
            Encoding.UTF8.GetBytes(trimmed.Normalize(NormalizationForm.FormKC))).ToUpperInvariant();
    }

    private static string CreateRandomToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }

    private static string HashSecret(string secret)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(bytes);
    }
}
