namespace SciFiLarpNetwork.Backend.Domain.Events.PlayerEvents;

/// <summary>
/// Appended to the player's stream whenever one of the player's characters is created or becomes Active, so the
/// command conflicts on the player's version with a concurrent deactivation, which would otherwise miss the
/// character and leave it Active under an Inactive player.
/// </summary>
public sealed record PlayerCharacterActivated(Guid PlayerId, Guid CharacterId, DateTimeOffset OccurredAtUtc);
