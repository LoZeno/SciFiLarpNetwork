namespace SciFiLarpNetwork.Backend.Application.LarpEvents;

/// <param name="ChangedBy">The administrator account that gave the change, when it was recorded.</param>
/// <param name="IsClosingAward">Whether the change was given by closing the event.</param>
public sealed record ExperienceChangeSummary(int Amount, string? ChangedBy, DateTimeOffset ChangedAtUtc, bool IsClosingAward);
