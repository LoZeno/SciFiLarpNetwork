namespace SciFiLarpNetwork.Backend.Application.Administration;

public enum AdministratorOperationResult
{
    Succeeded,
    InvalidGrant,
    AlreadyInitialized,
    AccountNotFound,
    AccountNameAlreadyUsed,
    LastAdministrator,
    ActorNoLongerAuthorized
}
