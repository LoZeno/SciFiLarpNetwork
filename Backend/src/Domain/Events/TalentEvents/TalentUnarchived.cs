namespace SciFiLarpNetwork.Backend.Domain.Events.TalentEvents;

public sealed record TalentUnarchived(Guid TalentId, DateTimeOffset OccurredAtUtc);
