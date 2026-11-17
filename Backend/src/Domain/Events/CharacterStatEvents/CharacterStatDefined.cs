using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Domain.Events.CharacterStatEvents;

public sealed record CharacterStatDefined(
    Guid StatId,
    string Name,
    string NormalizedName,
    CharacterStatType Type,
    DateTimeOffset OccurredAtUtc);
