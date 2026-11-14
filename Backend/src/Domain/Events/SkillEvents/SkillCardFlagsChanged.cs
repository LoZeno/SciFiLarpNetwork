namespace SciFiLarpNetwork.Backend.Domain.Events.SkillEvents;

public sealed record SkillCardFlagsChanged(Guid SkillId, Guid[] CardFlagIds, DateTimeOffset OccurredAtUtc);
