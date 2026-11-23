namespace SciFiLarpNetwork.Management.Api.Messages;

internal sealed record MessageSummaryProperties(
    Guid MessageId,
    MessageCharacterProperties Sender,
    int RecipientCount,
    string Title,
    DateTimeOffset SentAtUtc,
    Guid? ReplyToMessageId);
