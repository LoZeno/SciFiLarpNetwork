namespace SciFiLarpNetwork.Backend.Application.Administration;

/// <summary>
/// Administrator account directory: lists accounts and lets an administrator create accounts and change roles.
/// </summary>
public interface IAdministratorAccountService
{
    Task<IReadOnlyList<AdministratorSummary>> GetAdministratorsAsync(
        CancellationToken cancellationToken = default);

    Task<AdministratorAccountCreationResult> CreateAccountAsync(
        Guid actorAccountId,
        string userName,
        string password,
        string role,
        CancellationToken cancellationToken = default);

    Task<AdministratorOperationResult> ChangeRoleAsync(
        Guid actorAccountId,
        Guid targetAccountId,
        string role,
        CancellationToken cancellationToken = default);
}
