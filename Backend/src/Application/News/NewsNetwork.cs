namespace SciFiLarpNetwork.Backend.Application.News;

/// <summary>A story's network as shown now; one that cannot be found has no name.</summary>
public sealed record NewsNetwork(Guid NetworkId, bool IsKnown, string? Name, bool IsEnabled);
