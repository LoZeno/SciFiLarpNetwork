using JasperFx;
using Marten;
using Microsoft.AspNetCore.Identity;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;

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
        var actor = await session.Events.FetchForWriting<AdministratorAccountDocument>(actorAccountId, cancellationToken);
        if (actor.Aggregate is not { Role: AdministratorRoles.Administrator })
        {
            return new AdministratorAccountCreationResult(AdministratorOperationResult.ActorNoLongerAuthorized, null);
        }

        // Guards against the actor being demoted concurrently even though nothing is appended
        // to the actor's own stream (Marten "Enforcing Consistency Without Appending Events").
        actor.AlwaysEnforceConsistency = true;

        if (await session.FindAccountByUserNameAsync(userName, cancellationToken) is not null)
        {
            return new AdministratorAccountCreationResult(AdministratorOperationResult.AccountNameAlreadyUsed, null);
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
                role,
                now));
        session.StartAuditStream("administrator", actorAccountId, accountId, "AdministratorAccountCreated", "none", role, now);

        try
        {
            return await session.TrySaveChangesAsync(cancellationToken)
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
        if (!IsKnownRole(role))
        {
            return AdministratorOperationResult.InvalidGrant;
        }

        await using var session = sessions.OpenSession();
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

        if (previousRole == AdministratorRoles.Administrator &&
            role == AdministratorRoles.Reader &&
            await CountAdministratorsAsync(session, cancellationToken) <= 1)
        {
            return AdministratorOperationResult.LastAdministrator;
        }

        actor.AlwaysEnforceConsistency = true;
        var now = timeProvider.GetUtcNow();
        target.AppendOne(new AccountRoleChanged(targetAccountId, previousRole, role, actorAccountId, now));
        session.StartAuditStream("administrator", actorAccountId, targetAccountId, "AdministratorRoleChanged", previousRole, role, now);

        return await session.TrySaveChangesAsync(cancellationToken)
            ? AdministratorOperationResult.Succeeded
            : AdministratorOperationResult.ActorNoLongerAuthorized;
    }

    private static bool IsKnownRole(string role) =>
        role is AdministratorRoles.Reader or AdministratorRoles.Administrator;

    private static Task<int> CountAdministratorsAsync(IQuerySession session, CancellationToken cancellationToken) =>
        session.Query<AdministratorAccountDocument>()
            .Where(a => a.Role == AdministratorRoles.Administrator)
            .CountAsync(cancellationToken);
}
