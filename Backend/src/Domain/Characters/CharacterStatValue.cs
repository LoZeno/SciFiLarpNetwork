namespace SciFiLarpNetwork.Backend.Domain.Characters;

/// <summary>A character's value for one stat: <see cref="Number"/> for a Number stat, <see cref="Text"/> for a Text stat.</summary>
public sealed record CharacterStatValue(Guid StatId, int? Number, string? Text);
