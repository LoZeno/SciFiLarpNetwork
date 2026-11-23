namespace SciFiLarpNetwork.Backend.Domain.Events.MessageEvents;

/// <param name="RecipientIds">The recipients in the order they were given, each once.</param>
/// <param name="Body">The body exactly as typed.</param>
/// <param name="ReplyToMessageId">The message this one replies to; <see langword="null"/> for a new message.</param>
public sealed record MessageSent(
    Guid MessageId,
    Guid SenderId,
    IReadOnlyList<Guid> RecipientIds,
    string Title,
    string Body,
    Guid? ReplyToMessageId,
    DateTimeOffset OccurredAtUtc);
