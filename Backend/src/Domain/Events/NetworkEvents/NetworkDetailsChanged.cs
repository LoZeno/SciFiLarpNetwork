using SciFiLarpNetwork.Backend.Domain.Networks;

namespace SciFiLarpNetwork.Backend.Domain.Events.NetworkEvents;

public sealed record NetworkDetailsChanged(
    Guid NetworkId,
    string Name,
    string NormalizedName,
    string? Description,
    NetworkTheme Theme,
    DateTimeOffset OccurredAtUtc);
