namespace SciFiLarpNetwork.Management.Api.Players;

internal sealed record CharacterSkillPurchaseProperties(
    Guid SkillId,
    string Name,
    Guid TalentId,
    int Count,
    string? Specifics,
    int Cost,
    bool IsMultiPurchase,
    bool IsArchived);
