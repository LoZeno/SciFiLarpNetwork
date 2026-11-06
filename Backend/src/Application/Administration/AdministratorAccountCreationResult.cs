namespace SciFiLarpNetwork.Backend.Application.Administration;

public sealed record AdministratorAccountCreationResult(
    AdministratorOperationResult Result,
    AdministratorSummary? Account);
