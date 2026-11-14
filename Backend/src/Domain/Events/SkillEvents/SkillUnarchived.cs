namespace SciFiLarpNetwork.Backend.Domain.Events.SkillEvents;

public sealed record SkillUnarchived(Guid SkillId, DateTimeOffset OccurredAtUtc);
