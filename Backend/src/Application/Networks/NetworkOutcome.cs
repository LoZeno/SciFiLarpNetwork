namespace SciFiLarpNetwork.Backend.Application.Networks;

public enum NetworkOutcome
{
    Succeeded,
    NotFound,
    StaleVersion,
    InvalidName,
    NameAlreadyUsed,
    InvalidDescription,
    InvalidCode,
    CodeAlreadyUsed,
    InvalidTheme,
    InvalidImage,
    ImageTooLarge,
    ImageDimensionsTooLarge,
    NetworkDisabled,
    DefaultNetworkCannotBeDisabled
}
