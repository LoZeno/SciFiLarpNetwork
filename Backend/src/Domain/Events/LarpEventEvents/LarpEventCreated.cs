namespace SciFiLarpNetwork.Backend.Domain.Events.LarpEventEvents;

/// <param name="BaseExperience">
/// The experience points every chosen participant can get when the event closes; <see langword="null"/> for none.
/// </param>
public sealed record LarpEventCreated(
    Guid EventId,
    int Number,
    string Name,
    string NormalizedName,
    string? Description,
    int? BaseExperience,
    DateTimeOffset OccurredAtUtc);
