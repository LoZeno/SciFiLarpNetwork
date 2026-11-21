using SciFiLarpNetwork.Backend.Domain.LarpEvents;

namespace SciFiLarpNetwork.Backend.Application.LarpEvents;

/// <param name="BaseExperience">The experience every chosen participant can get when the event closes.</param>
/// <param name="FirstDate">The earliest day, or <see langword="null"/> when the event has no days.</param>
/// <param name="LastDate">The latest day, or <see langword="null"/> when the event has no days.</param>
/// <param name="Days">The event's days in date order.</param>
/// <param name="ParticipantCount">The number of characters registered for at least one day.</param>
public sealed record LarpEventSummary(
    Guid EventId,
    int Number,
    string Name,
    string? Description,
    LarpEventStatus Status,
    bool IsArchived,
    int? BaseExperience,
    DateOnly? FirstDate,
    DateOnly? LastDate,
    IReadOnlyList<LarpEventDaySummary> Days,
    int ParticipantCount,
    long Version);
