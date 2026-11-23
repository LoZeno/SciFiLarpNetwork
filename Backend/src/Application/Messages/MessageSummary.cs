namespace SciFiLarpNetwork.Backend.Application.Messages;

/// <summary>A message as lists show it: without its body.</summary>
public sealed record MessageSummary(
    Guid MessageId,
    MessageCharacter Sender,
    int RecipientCount,
    string Title,
    DateTimeOffset SentAtUtc,
    Guid? ReplyToMessageId);
