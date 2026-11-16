using JasperFx;
using Marten;
using Microsoft.AspNetCore.Identity;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

internal sealed class AdministratorAccountService(
    AttributedSessionFactory sessions,
    IPasswordHasher<object> passwordHasher,
    TimeProvider timeProvider) : IAdministratorAccountService
{
    private readonly object _passwordSubject = new();

    public async Task<IReadOnlyList<AdministratorSummary>> GetAdministratorsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var query = sessions.QuerySession();
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
        if (!AdministratorCredentialRules.AreValid(userName, password) || !IsKnownRole(role))
        {
            return new AdministratorAccountCreationResult(AdministratorOperationResult.InvalidGrant, null);
        }

        await using var session = sessions.OpenSession();
        var actor = await session.Events.FetchForWriting<AdministratorAccountDocument>(actorAccountId,
            cancellationToken);
        if (actor.Aggregate is not { Role: AdministratorRoles.Administrator })
        {
            return new AdministratorAccountCreationResult(AdministratorOperationResult.ActorNoLongerAuthorized, null);
        }

        if (await session.FindAccountByUserNameAsync(userName, cancellationToken) is not null)
        {
            return new AdministratorAccountCreationResult(AdministratorOperationResult.AccountNameAlreadyUsed, null);
        }

        // Appending to the actor's own stream makes a concurrent demotion of the actor (which appends there too)
        // conflict with this command. Only checking the actor's version cannot see a demotion that commits at the
        // same moment.
        var accountId = Guid.NewGuid();
        var now = timeProvider.GetUtcNow();
        actor.AppendOne(new AccountAuthorityUsed(actorAccountId, "AdministratorAccountCreated", now));
        session.Events.StartStream<AdministratorAccountDocument>(
            accountId,
            new AccountCreated(
                accountId,
                userName.Trim(),
                AdministratorCredentialRules.NormalizeUserName(userName),
                passwordHasher.HashPassword(_passwordSubject, password),
                role,
                now));
        session.StartAuditStream("administrator", actorAccountId, accountId, "AdministratorAccountCreated", "none",
            role, now);

        try
        {
            return await session.TrySaveChangesAsync(cancellationToken)
                ? new AdministratorAccountCreationResult(
                    AdministratorOperationResult.Succeeded,
                    new AdministratorSummary(accountId, userName.Trim(), role, now))
                : new AdministratorAccountCreationResult(AdministratorOperationResult.ConcurrentChange, null);
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
        if (!IsKnownRole(role))
        {
            return AdministratorOperationResult.InvalidGrant;
        }

        await using var session = sessions.OpenSession();
        var actor = await session.Events.FetchForWriting<AdministratorAccountDocument>(actorAccountId,
            cancellationToken);
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

        if (previousRole == AdministratorRoles.Administrator &&
            role == AdministratorRoles.Reader &&
            await CountAdministratorsAsync(session, cancellationToken) <= 1)
        {
            return AdministratorOperationResult.LastAdministrator;
        }

        // Every role change appends to the single roster stream, so concurrent role changes conflict there and
        // cannot together remove the last administrator. The actor's own stream gets an authority event (unless it
        // is the target), so a concurrent demotion of the actor conflicts too.
        var now = timeProvider.GetUtcNow();
        var roster =
            await session.Events.FetchForWriting<AdministratorRosterDocument>(AdministratorRosterDocument.StreamId,
                cancellationToken);
        roster.AppendOne(new AdministratorRosterChanged(actorAccountId, targetAccountId, now));
        if (actor != target)
        {
            actor.AppendOne(new AccountAuthorityUsed(actorAccountId, "AdministratorRoleChanged", now));
        }

        target.AppendOne(new AccountRoleChanged(targetAccountId, previousRole, role, actorAccountId, now));
        session.StartAuditStream("administrator", actorAccountId, targetAccountId, "AdministratorRoleChanged",
            previousRole, role, now);

        try
        {
            return await session.TrySaveChangesAsync(cancellationToken)
                ? AdministratorOperationResult.Succeeded
                : AdministratorOperationResult.ConcurrentChange;
        }
        catch (Marten.Exceptions.ExistingStreamIdCollisionException)
        {
            // Two first-ever role changes both started the roster stream; the later one lost.
            return AdministratorOperationResult.ConcurrentChange;
        }
    }

    private static bool IsKnownRole(string role) =>
        role is AdministratorRoles.Reader or AdministratorRoles.Administrator;

    private static Task<int> CountAdministratorsAsync(IQuerySession session, CancellationToken cancellationToken) =>
        session.Query<AdministratorAccountDocument>()
            .Where(a => a.Role == AdministratorRoles.Administrator)
            .CountAsync(cancellationToken);
}
