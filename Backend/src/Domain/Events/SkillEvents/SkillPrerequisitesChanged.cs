namespace SciFiLarpNetwork.Backend.Domain.Events.SkillEvents;

public sealed record SkillPrerequisitesChanged(Guid SkillId, Guid[][] PrerequisiteGroups, DateTimeOffset OccurredAtUtc);
