namespace SciFiLarpNetwork.Backend.Application.Networks;

public enum NetworkOutcome
{
    Succeeded,
    NotFound,
    StaleVersion,
    InvalidName,
    NameAlreadyUsed,
    InvalidCode,
    CodeAlreadyUsed,
    InvalidTheme,
    InvalidImage,
    ImageTooLarge,
    ImageDimensionsTooLarge,
    NetworkDisabled,
    DefaultNetworkCannotBeDisabled
}
