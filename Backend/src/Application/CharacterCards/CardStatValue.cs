namespace SciFiLarpNetwork.Backend.Application.CharacterCards;

/// <param name="Value">The stat value as printed: a Number stat's whole number, or a Text stat's text.</param>
public sealed record CardStatValue(string Name, string Value);
