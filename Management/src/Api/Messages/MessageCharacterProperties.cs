namespace SciFiLarpNetwork.Management.Api.Messages;

/// <param name="IsKnown">Whether a character has this identifier; otherwise number and name are missing.</param>
internal sealed record MessageCharacterProperties(Guid CharacterId, bool IsKnown, int? Number, string? Name, bool IsArchived);
