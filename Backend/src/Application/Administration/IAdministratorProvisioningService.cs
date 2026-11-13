namespace SciFiLarpNetwork.Backend.Application.Administration;

/// <summary>
/// Operator-only provisioning: issues one-time bootstrap and recovery credentials and consumes them
/// to create the first administrator or reset an administrator's password.
/// </summary>
public interface IAdministratorProvisioningService
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
}
