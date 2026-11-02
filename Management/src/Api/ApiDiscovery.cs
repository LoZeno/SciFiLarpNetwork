using SciFiLarpNetwork.Management.Api.Siren;

namespace SciFiLarpNetwork.Management.Api;

public sealed record ApiDiscovery(
    SirenEntity ApiResource,
    IReadOnlyList<SirenLink> NavigationLinks);
