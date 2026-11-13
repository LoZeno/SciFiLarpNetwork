namespace SciFiLarpNetwork.Backend.Application.Administration;

/// <summary>
/// Administrator sign-in sessions: sign-in, validation, renewal, and revocation of bearer tokens.
/// </summary>
public interface IAdministratorSessionService
{
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
}
