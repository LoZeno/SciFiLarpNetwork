namespace SciFiLarpNetwork.Backend.Domain.Events.LarpEventEvents;

/// <param name="DayIds">Only days the character was registered for.</param>
public sealed record ParticipantUnregistered(
    Guid EventId,
    Guid CharacterId,
    IReadOnlyList<Guid> DayIds,
    DateTimeOffset OccurredAtUtc);
