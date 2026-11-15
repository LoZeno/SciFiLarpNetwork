namespace SciFiLarpNetwork.Management.Api.Networks;

internal sealed record NetworkProperties(
    Guid NetworkId,
    string Code,
    string Name,
    string? Description,
    string Theme,
    NetworkImageProperties? BackgroundImage,
    bool IsEnabled,
    bool IsDefault,
    long Version);
