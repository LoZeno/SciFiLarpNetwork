using SciFiLarpNetwork.Backend.Domain.CharacterSkills;

namespace SciFiLarpNetwork.Backend.Domain.Events.CharacterEvents;

/// <summary>The character's talents and skills were replaced by these lists.</summary>
public sealed record CharacterSkillsChanged(
    Guid CharacterId,
    IReadOnlyList<Guid> TalentIds,
    IReadOnlyList<SkillPurchase> Purchases,
    DateTimeOffset OccurredAtUtc);
