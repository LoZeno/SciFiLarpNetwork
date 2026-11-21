namespace SciFiLarpNetwork.Backend.Application.CharacterCards;

/// <summary>The dates a card printed for an event is valid for: the character's first and last registered day.</summary>
public sealed record CardValidity(DateOnly From, DateOnly To);
