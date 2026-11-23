namespace SciFiLarpNetwork.Management.Tests.ViewModels.Messages;

internal sealed record FakeMessage(
    Guid Id,
    Guid SenderId,
    IReadOnlyList<Guid> RecipientIds,
    string Title,
    string Body,
    DateTimeOffset SentAtUtc,
    Guid? ReplyToMessageId);
