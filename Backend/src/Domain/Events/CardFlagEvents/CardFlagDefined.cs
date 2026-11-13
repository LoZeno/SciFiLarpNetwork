namespace SciFiLarpNetwork.Backend.Domain.Events.CardFlagEvents;

public sealed record CardFlagDefined(
    Guid CardFlagId,
    string Name,
    string NormalizedName,
    DateTimeOffset OccurredAtUtc);
