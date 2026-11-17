namespace SciFiLarpNetwork.Backend.Domain.Events.CharacterEvents;

public sealed record CharacterArchived(Guid CharacterId, DateTimeOffset OccurredAtUtc);
