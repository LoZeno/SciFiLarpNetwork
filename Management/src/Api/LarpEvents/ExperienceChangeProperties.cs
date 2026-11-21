namespace SciFiLarpNetwork.Management.Api.LarpEvents;

internal sealed record ExperienceChangeProperties(int Amount, string? ChangedBy, DateTimeOffset ChangedAtUtc, bool IsClosingAward);
