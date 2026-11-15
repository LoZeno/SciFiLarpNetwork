using SciFiLarpNetwork.Backend.Domain.Networks;

namespace SciFiLarpNetwork.Backend.Application.Networks;

/// <param name="BackgroundImage">The uploaded background image, or <see langword="null"/> to use the theme's image.</param>
public sealed record NetworkSummary(
    Guid NetworkId,
    string Code,
    string Name,
    string? Description,
    NetworkTheme Theme,
    NetworkImageReference? BackgroundImage,
    bool IsEnabled,
    bool IsDefault,
    long Version);
