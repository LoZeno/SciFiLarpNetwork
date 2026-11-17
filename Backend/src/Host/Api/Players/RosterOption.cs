namespace SciFiLarpNetwork.Backend.Api.Players;

/// <summary>A fixed choice (a player type, character status, or stat type) with its localized title.</summary>
public sealed record RosterOption(string Name, string Title);
