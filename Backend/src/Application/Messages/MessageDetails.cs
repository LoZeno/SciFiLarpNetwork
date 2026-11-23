namespace SciFiLarpNetwork.Backend.Application.Messages;

/// <param name="Recipients">The recipients in the order they were given.</param>
/// <param name="SentBy">The user name of the administrator who sent it, or the recorded actor when it is not a known account.</param>
/// <param name="ReplyTo">The message this one replies to; <see langword="null"/> for a new message.</param>
public sealed record MessageDetails(
    Guid MessageId,
    MessageCharacter Sender,
    IReadOnlyList<MessageCharacter> Recipients,
    string Title,
    string Body,
    DateTimeOffset SentAtUtc,
    string? SentBy,
    MessageReference? ReplyTo);
