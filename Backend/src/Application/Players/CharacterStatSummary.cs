using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Application.Players;

public sealed record CharacterStatSummary(Guid StatId, string Name, CharacterStatType Type, bool IsArchived, long Version);
