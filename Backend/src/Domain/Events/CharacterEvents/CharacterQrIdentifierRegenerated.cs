namespace SciFiLarpNetwork.Backend.Domain.Events.CharacterEvents;

public sealed record CharacterQrIdentifierRegenerated(Guid CharacterId, string QrIdentifier, DateTimeOffset OccurredAtUtc);
