namespace SciFiLarpNetwork.Backend.Application.Players;

/// <summary>A value for an existing stat: <see cref="Number"/> for a Number stat, <see cref="Text"/> for a Text stat.</summary>
public sealed record CharacterStatInput(Guid StatId, int? Number, string? Text);
