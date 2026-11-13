namespace SciFiLarpNetwork.Backend.Domain.Events.TalentEvents;

public sealed record TalentArchived(Guid TalentId, DateTimeOffset OccurredAtUtc);
