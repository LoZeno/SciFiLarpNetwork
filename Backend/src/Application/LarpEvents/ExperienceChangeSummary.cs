namespace SciFiLarpNetwork.Backend.Application.LarpEvents;

/// <param name="ChangedBy">The administrator account that gave the change, when it was recorded.</param>
public sealed record ExperienceChangeSummary(int Amount, string? ChangedBy, DateTimeOffset ChangedAtUtc);
