namespace SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;

/// <summary>
/// Appended to the acting administrator's own account stream when an action depends on that account still being an
/// administrator. A concurrent role change appends to the same stream, so the stream version check rejects one of them.
/// </summary>
public sealed record AccountAuthorityUsed(Guid AccountId, string Action, DateTimeOffset OccurredAtUtc);
