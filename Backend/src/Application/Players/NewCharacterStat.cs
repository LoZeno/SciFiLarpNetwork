using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Application.Players;

/// <summary>
/// A stat typed in the character dialog with this character's value. A new name defines the stat for every
/// character; a name that already exists uses that stat, which must have the same type.
/// </summary>
public sealed record NewCharacterStat(string Name, CharacterStatType Type, int? Number, string? Text);
