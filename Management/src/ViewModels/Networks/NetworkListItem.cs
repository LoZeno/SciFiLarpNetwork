namespace SciFiLarpNetwork.Management.ViewModels.Networks;

/// <param name="DisplayName">The name and code, with localized tags for the default and disabled networks.</param>
public sealed record NetworkListItem(
    Guid NetworkId,
    string Code,
    string Name,
    bool IsEnabled,
    bool IsDefault,
    string Href,
    string DisplayName);
