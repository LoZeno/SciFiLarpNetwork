namespace SciFiLarpNetwork.Backend.Domain.Events.LarpEventEvents;

/// <param name="DayIds">Only the days the character was not registered for yet.</param>
public sealed record ParticipantRegistered(
    Guid EventId,
    Guid CharacterId,
    IReadOnlyList<Guid> DayIds,
    DateTimeOffset OccurredAtUtc);
