namespace SciFiLarpNetwork.Backend.Domain.Events.CharacterEvents;

/// <param name="NetworkId">The character's non-default network, or <see langword="null"/> for none.</param>
public sealed record CharacterNetworkChanged(Guid CharacterId, Guid? NetworkId, DateTimeOffset OccurredAtUtc);
