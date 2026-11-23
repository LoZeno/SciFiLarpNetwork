namespace SciFiLarpNetwork.Backend.Infrastructure.Messages.MessageDocuments;

/// <param name="RecipientIds">The recipients in the order they were given, each once.</param>
/// <param name="SentBy">The recorded actor of the send: an administrator account identifier.</param>
public sealed record MessageDocument(
    Guid Id,
    Guid SenderId,
    Guid[] RecipientIds,
    string Title,
    string Body,
    Guid? ReplyToMessageId,
    DateTimeOffset SentAtUtc,
    string? SentBy);
