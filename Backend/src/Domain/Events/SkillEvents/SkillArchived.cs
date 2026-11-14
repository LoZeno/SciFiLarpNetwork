namespace SciFiLarpNetwork.Backend.Domain.Events.SkillEvents;

public sealed record SkillArchived(Guid SkillId, DateTimeOffset OccurredAtUtc);
