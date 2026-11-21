namespace SciFiLarpNetwork.Backend.Infrastructure.LarpEvents.LarpEventDocuments;

/// <param name="ChangedBy">The user name metadata of the event that recorded the change.</param>
/// <param name="IsClosingAward">Whether the change was given by closing the event.</param>
public sealed record LarpEventExperienceEntry(
    Guid CharacterId,
    int Amount,
    string? ChangedBy,
    DateTimeOffset ChangedAtUtc,
    bool IsClosingAward);
