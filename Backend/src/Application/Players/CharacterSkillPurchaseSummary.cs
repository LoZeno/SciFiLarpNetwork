namespace SciFiLarpNetwork.Backend.Application.Players;

/// <param name="Cost">The skill's current cost for one purchase.</param>
/// <param name="IsArchived">Whether the skill is archived; an archived skill is kept but cannot be bought again.</param>
public sealed record CharacterSkillPurchaseSummary(
    Guid SkillId,
    string Name,
    Guid TalentId,
    int Count,
    string? Specifics,
    int Cost,
    bool IsMultiPurchase,
    bool IsArchived);
