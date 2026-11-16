namespace SciFiLarpNetwork.Management.Api.Networks;

internal sealed record NetworkCollectionProperties(bool HasDefault, IReadOnlyList<NetworkThemeProperties> Themes);
