namespace SciFiLarpNetwork.Backend.Application.Messages;

/// <param name="MessageId">Chosen by the client before the first try, so a repeated submission is recognized.</param>
/// <param name="RecipientIds">The recipients; one listed twice is kept once.</param>
/// <param name="ReplyToMessageId">The message this one replies to, if any.</param>
public sealed record MessageDraft(
    Guid MessageId,
    Guid SenderId,
    IReadOnlyList<Guid> RecipientIds,
    string? Title,
    string? Body,
    Guid? ReplyToMessageId = null)
{
    /// <summary>The recipients in the order they were given, each once.</summary>
    public IReadOnlyList<Guid> DistinctRecipientIds => [.. RecipientIds.Distinct()];
}
