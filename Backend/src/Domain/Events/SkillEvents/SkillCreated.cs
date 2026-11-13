using SciFiLarpNetwork.Backend.Domain.Skills;

namespace SciFiLarpNetwork.Backend.Domain.Events.SkillEvents;

public sealed record SkillCreated(
    Guid SkillId,
    string Name,
    string NormalizedName,
    string? Description,
    int Cost,
    bool IsMultiPurchase,
    bool IsAdvanced,
    Guid[] TalentIds,
    GameFlag[] GameFlags,
    Guid[] CardFlagIds,
    Guid[][] PrerequisiteGroups,
    DateTimeOffset OccurredAtUtc);
