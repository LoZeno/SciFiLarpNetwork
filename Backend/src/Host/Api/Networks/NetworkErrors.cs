using SciFiLarpNetwork.Backend.Api.Skills;
using SciFiLarpNetwork.Backend.Application.Networks;

namespace SciFiLarpNetwork.Backend.Api.Networks;

/// <summary>
/// Maps network command outcomes to stable Siren error codes. Each code is also its localization key.
/// </summary>
public static class NetworkErrors
{
    public const string CodeAlreadyUsed = "errors.codeAlreadyUsed";
    public const string InvalidCode = "errors.invalidCode";
    public const string InvalidTheme = "errors.invalidTheme";
    public const string InvalidImage = "errors.invalidImage";
    public const string ImageTooLarge = "errors.imageTooLarge";
    public const string NetworkDisabled = "errors.networkDisabled";
    public const string DefaultNetworkCannotBeDisabled = "errors.defaultNetworkCannotBeDisabled";

    public static ApiException ToApiException(NetworkOutcome outcome) =>
        outcome switch
        {
            NetworkOutcome.NotFound => Create(StatusCodes.Status404NotFound, ApiErrorCodes.NotFound),
            NetworkOutcome.StaleVersion => Create(StatusCodes.Status409Conflict, CatalogErrors.StaleVersion),
            NetworkOutcome.NameAlreadyUsed => Create(StatusCodes.Status409Conflict, CatalogErrors.NameAlreadyUsed),
            NetworkOutcome.CodeAlreadyUsed => Create(StatusCodes.Status409Conflict, CodeAlreadyUsed),
            NetworkOutcome.NetworkDisabled => Create(StatusCodes.Status409Conflict, NetworkDisabled),
            NetworkOutcome.DefaultNetworkCannotBeDisabled => Create(StatusCodes.Status409Conflict, DefaultNetworkCannotBeDisabled),
            NetworkOutcome.InvalidName => Create(StatusCodes.Status400BadRequest, CatalogErrors.InvalidName),
            NetworkOutcome.InvalidCode => Create(StatusCodes.Status400BadRequest, InvalidCode),
            NetworkOutcome.InvalidTheme => Create(StatusCodes.Status400BadRequest, InvalidTheme),
            NetworkOutcome.InvalidImage => Create(StatusCodes.Status400BadRequest, InvalidImage),
            NetworkOutcome.ImageTooLarge => Create(StatusCodes.Status400BadRequest, ImageTooLarge),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "A successful outcome is not an error.")
        };

    public static NetworkSummary Unwrap(NetworkResult result) =>
        result.Outcome == NetworkOutcome.Succeeded && result.Value is { } value
            ? value
            : throw ToApiException(result.Outcome);

    private static ApiException Create(int statusCode, string code) => new(statusCode, code, code);
}
