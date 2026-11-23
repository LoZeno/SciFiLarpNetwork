namespace SciFiLarpNetwork.Management.Api.Messages;

internal sealed record MessageDetailsProperties(
    Guid MessageId,
    MessageCharacterProperties Sender,
    IReadOnlyList<MessageCharacterProperties> Recipients,
    string Title,
    string Body,
    DateTimeOffset SentAtUtc,
    string? SentBy,
    MessageReferenceProperties? ReplyTo);
