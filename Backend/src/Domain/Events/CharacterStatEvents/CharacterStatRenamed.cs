namespace SciFiLarpNetwork.Backend.Domain.Events.CharacterStatEvents;

public sealed record CharacterStatRenamed(Guid StatId, string Name, string NormalizedName, DateTimeOffset OccurredAtUtc);
