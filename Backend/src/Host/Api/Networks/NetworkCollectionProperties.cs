namespace SciFiLarpNetwork.Backend.Api.Networks;

/// <param name="Themes">Every product-defined theme with its localized title, for network dialogs.</param>
/// <param name="HasDefault">Whether an enabled default network exists; clients warn when it does not.</param>
public sealed record NetworkCollectionProperties(
    string Title,
    string Description,
    bool IncludesDisabled,
    bool HasDefault,
    IReadOnlyList<NetworkThemeOption> Themes);
