namespace SciFiLarpNetwork.Backend.Domain.Events.SkillEvents;

public sealed record SkillDetailsChanged(
    Guid SkillId,
    string Name,
    string NormalizedName,
    string? Description,
    int Cost,
    bool IsMultiPurchase,
    bool IsAdvanced,
    DateTimeOffset OccurredAtUtc);
