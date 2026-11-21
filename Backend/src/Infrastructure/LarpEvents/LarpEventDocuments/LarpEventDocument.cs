using SciFiLarpNetwork.Backend.Domain.LarpEvents;

namespace SciFiLarpNetwork.Backend.Infrastructure.LarpEvents.LarpEventDocuments;

public sealed record LarpEventDocument(
    Guid Id,
    int Number,
    string Name,
    string NormalizedName,
    string? Description,
    LarpEventStatus Status,
    bool IsArchived)
{
    /// <summary>The event stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }

    /// <summary>The experience every chosen participant can get when the event closes; <see langword="null"/> for none.</summary>
    public int? BaseExperience { get; init; }

    /// <summary>The closing experience already given, by character, so a later close never gives it again.</summary>
    public LarpEventGivenAward[] GivenAwards { get; init; } = [];

    /// <summary>The days in date order.</summary>
    public LarpEventDay[] Days { get; init; } = [];

    /// <summary>
    /// The first day as <see cref="DateOnly.DayNumber"/>, so the list can be ordered by it in the database;
    /// <see langword="null"/> when the event has no days.
    /// </summary>
    public int? FirstDayNumber { get; init; }

    /// <summary>The characters registered for at least one day.</summary>
    public LarpEventParticipant[] Participants { get; init; } = [];

    /// <summary>Every experience change given at the event, oldest first.</summary>
    public LarpEventExperienceEntry[] ExperienceChanges { get; init; } = [];
}
