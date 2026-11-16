namespace SciFiLarpNetwork.Backend.Domain.Events.SkillEvents;

/// <summary>
/// Appended to the single prerequisite-graph stream whenever a skill's prerequisites change, so concurrent
/// prerequisite changes conflict on that stream's version and cannot together close a loop.
/// </summary>
public sealed record PrerequisiteGraphChanged(Guid SkillId, DateTimeOffset OccurredAtUtc);
