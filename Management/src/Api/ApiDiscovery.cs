using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Management.Api;

public sealed record ApiDiscovery(
    SirenEntity ApiResource,
    IReadOnlyList<SirenLink> NavigationLinks);
