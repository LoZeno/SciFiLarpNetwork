namespace SciFiLarpNetwork.Backend.Infrastructure.LarpEvents.LarpEventDocuments;

/// <param name="ChangedBy">The user name metadata of the event that recorded the change.</param>
public sealed record LarpEventExperienceEntry(Guid CharacterId, int Amount, string? ChangedBy, DateTimeOffset ChangedAtUtc);
