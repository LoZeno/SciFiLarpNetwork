namespace SciFiLarpNetwork.Backend.Domain.Events.SkillEvents;

public sealed record SkillTalentsChanged(Guid SkillId, Guid[] TalentIds, DateTimeOffset OccurredAtUtc);
