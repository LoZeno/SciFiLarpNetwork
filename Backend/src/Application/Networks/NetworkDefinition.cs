using SciFiLarpNetwork.Backend.Domain.Networks;

namespace SciFiLarpNetwork.Backend.Application.Networks;

/// <summary>The editable details of a network. The code is set once, at creation.</summary>
public sealed record NetworkDefinition(string Name, string? Description, NetworkTheme Theme);
