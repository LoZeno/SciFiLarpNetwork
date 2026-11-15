namespace SciFiLarpNetwork.Backend.Application.Administration;

public enum AdministratorOperationResult
{
    Succeeded,
    InvalidGrant,
    AlreadyInitialized,
    AccountNotFound,
    AccountNameAlreadyUsed,
    LastAdministrator,
    ActorNoLongerAuthorized,

    /// <summary>
    /// Another administrator command committed first and the change was not saved; retrying re-checks every rule.
    /// </summary>
    ConcurrentChange
}
