namespace SciFiLarpNetwork.Backend.Domain.Events.CharacterStatEvents;

public sealed record CharacterStatArchived(Guid StatId, DateTimeOffset OccurredAtUtc);
