namespace SciFiLarpNetwork.Backend.Domain.Events.MessageEvents;

/// <summary>
/// An administrator account opened the message's details, including its body. The account is the event's recorded
/// user name.
/// </summary>
public sealed record MessageOpened(Guid MessageId, DateTimeOffset OccurredAtUtc);
