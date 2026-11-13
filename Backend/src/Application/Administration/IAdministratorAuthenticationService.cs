namespace SciFiLarpNetwork.Backend.Application.Administration;

public interface IAdministratorAuthenticationService
{
    Task<string> IssueBootstrapCredentialAsync(CancellationToken cancellationToken = default);

    Task<string> IssueRecoveryCredentialAsync(CancellationToken cancellationToken = default);

    Task<AdministratorOperationResult> BootstrapAsync(
        string credential,
        string userName,
        string password,
        CancellationToken cancellationToken = default);

    Task<AdministratorOperationResult> RecoverAsync(
        string credential,
        string userName,
        string password,
        CancellationToken cancellationToken = default);

    Task<AdministratorSession?> SignInAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default);

    Task<AdministratorSession?> ValidateSessionAsync(
        string sessionToken,
        CancellationToken cancellationToken = default);

    Task<AdministratorSession?> RenewSessionAsync(
        string sessionToken,
        CancellationToken cancellationToken = default);

    Task RevokeSessionAsync(string sessionToken, CancellationToken cancellationToken = default);

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
