namespace SciFiLarpNetwork.Backend.Application.Messages;

/// <summary>A sender or recipient with its current running number and name.</summary>
/// <param name="IsKnown">
/// <see langword="false"/> when no character has this identifier; <see cref="Number"/> and <see cref="Name"/> are then
/// <see langword="null"/>.
/// </param>
public sealed record MessageCharacter(Guid CharacterId, bool IsKnown, int? Number, string? Name, bool IsArchived);
