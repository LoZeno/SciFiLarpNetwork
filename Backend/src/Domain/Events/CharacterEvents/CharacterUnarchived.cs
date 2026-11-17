namespace SciFiLarpNetwork.Backend.Domain.Events.CharacterEvents;

public sealed record CharacterUnarchived(Guid CharacterId, DateTimeOffset OccurredAtUtc);
