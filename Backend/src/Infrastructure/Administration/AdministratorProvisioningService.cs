using System.Security.Cryptography;
using Marten;
using Microsoft.AspNetCore.Identity;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

internal sealed class AdministratorProvisioningService(
    AttributedSessionFactory sessions,
    IPasswordHasher<object> passwordHasher,
    TimeProvider timeProvider) : IAdministratorProvisioningService
{
    private const string BootstrapPurpose = "bootstrap";
    private const string RecoveryPurpose = "recovery";
    private static readonly TimeSpan CredentialLifetime = TimeSpan.FromMinutes(15);
    private readonly object _passwordSubject = new();

    // All grants share one stream so that issuing and consuming them is serialized by its version.
    // The id spells "SFLN" "AU" "TH" followed by 1.
    private static Guid GrantsStreamId => new(0x53464C4E, 0x4155, 0x5448, [0, 0, 0, 0, 0, 0, 0, 1]);

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
        if (!AdministratorCredentialRules.AreValid(userName, password))
        {
            return AdministratorOperationResult.InvalidGrant;
        }

        await using var session = sessions.OpenSession();
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
                AdministratorCredentialRules.NormalizeUserName(userName),
                passwordHasher.HashPassword(_passwordSubject, password),
                AdministratorRoles.Administrator,
                now));
        grant.AppendOne(new GrantConsumed(accountId, now));
        session.StartAuditStream("local-operator", null, accountId, "AdministratorBootstrapped", "none", AdministratorRoles.Administrator, now);

        // A concurrent command that consumed the same grant first makes the grant stream
        // version stale, so this commit is rejected and nothing is written.
        return await session.TrySaveChangesAsync(cancellationToken)
            ? AdministratorOperationResult.Succeeded
            : AdministratorOperationResult.InvalidGrant;
    }

    public async Task<AdministratorOperationResult> RecoverAsync(
        string credential,
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (!AdministratorCredentialRules.AreValid(userName, password))
        {
            return AdministratorOperationResult.InvalidGrant;
        }

        await using var session = sessions.OpenSession();
        var grant = await session.Events.FetchForWriting<AdministratorGrantDocument>(GrantsStreamId, cancellationToken);
        if (!IsUsableGrant(grant.Aggregate, RecoveryPurpose, credential))
        {
            return AdministratorOperationResult.InvalidGrant;
        }

        var existing = await session.FindAccountByUserNameAsync(userName, cancellationToken);
        if (existing is null)
        {
            return AdministratorOperationResult.AccountNotFound;
        }

        var account = await session.Events.FetchForWriting<AdministratorAccountDocument>(existing.Id, cancellationToken);
        if (account.Aggregate is not { } current)
        {
            return AdministratorOperationResult.AccountNotFound;
        }

        var now = timeProvider.GetUtcNow();
        account.AppendOne(new AccountPasswordChanged(current.Id, passwordHasher.HashPassword(_passwordSubject, password), now));
        await RevokeAllSessionsAsync(session, current.Id, now, cancellationToken);
        grant.AppendOne(new GrantConsumed(current.Id, now));
        session.StartAuditStream("local-operator", null, current.Id, "AdministratorCredentialRecovered", current.Role, current.Role, now);

        return await session.TrySaveChangesAsync(cancellationToken)
            ? AdministratorOperationResult.Succeeded
            : AdministratorOperationResult.InvalidGrant;
    }

    private static async Task RevokeAllSessionsAsync(
        IDocumentSession session,
        Guid accountId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var sessionIds = await session.Query<AdministratorSessionDocument>()
            .Where(s => s.AccountId == accountId)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
        if (sessionIds.Count == 0)
        {
            return;
        }

        var sessionStreams = await session.Events.FetchManyForWriting<AdministratorSessionDocument>(sessionIds.ToArray(), cancellationToken);
        foreach (var sessionStream in sessionStreams)
        {
            if (sessionStream.Aggregate is { } existingSession)
            {
                sessionStream.AppendOne(new SessionRevoked(existingSession.Id, accountId, now));
            }
        }
    }

    private async Task<string> IssueCredentialAsync(
        bool requireAccounts,
        string rejectionMessage,
        Func<string, DateTimeOffset, DateTimeOffset, object> createEvent,
        CancellationToken cancellationToken)
    {
        await using var session = sessions.OpenSession();
        var hasAccounts = await session.Query<AdministratorAccountDocument>().AnyAsync(cancellationToken);
        if (hasAccounts != requireAccounts)
        {
            throw new InvalidOperationException(rejectionMessage);
        }

        var credential = AdministratorSecrets.CreateRandomToken();
        var now = timeProvider.GetUtcNow();
        var grant = await session.Events.FetchForWriting<AdministratorGrantDocument>(GrantsStreamId, cancellationToken);
        grant.AppendOne(createEvent(AdministratorSecrets.HashSecret(credential), now.Add(CredentialLifetime), now));
        await session.SaveChangesAsync(cancellationToken);

        return credential;
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
                Convert.FromHexString(AdministratorSecrets.HashSecret(credential)));
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
