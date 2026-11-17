namespace SciFiLarpNetwork.Backend.Domain.Events.CharacterStatEvents;

public sealed record CharacterStatUnarchived(Guid StatId, DateTimeOffset OccurredAtUtc);
