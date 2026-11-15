using SciFiLarpNetwork.Backend.Domain.Networks;

namespace SciFiLarpNetwork.Backend.Domain.Events.NetworkEvents;

public sealed record NetworkCreated(
    Guid NetworkId,
    string Code,
    string Name,
    string NormalizedName,
    string? Description,
    NetworkTheme Theme,
    DateTimeOffset OccurredAtUtc);
