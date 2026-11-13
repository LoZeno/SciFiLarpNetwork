using SciFiLarpNetwork.Backend.Domain.Skills;

namespace SciFiLarpNetwork.Backend.Domain.Events.SkillEvents;

public sealed record SkillGameFlagsChanged(Guid SkillId, GameFlag[] GameFlags, DateTimeOffset OccurredAtUtc);
