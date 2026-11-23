using SciFiLarpNetwork.Backend.Domain.Messages;

namespace SciFiLarpNetwork.Backend.Application.Messages;

public static class MessageDraftValidator
{
    /// <summary>Checks the rules that need no stored data.</summary>
    /// <returns><see langword="null"/> when the draft is valid; otherwise the outcome of the first failed check.</returns>
    public static MessageOutcome? Validate(MessageDraft draft) =>
        draft.MessageId == Guid.Empty ? MessageOutcome.InvalidMessageId
        : !MessageTexts.IsValidTitle(draft.Title) ? MessageOutcome.InvalidTitle
        : !MessageTexts.IsValidBody(draft.Body) ? MessageOutcome.InvalidBody
        : draft.DistinctRecipientIds.Count == 0 ? MessageOutcome.NoRecipients
        : draft.DistinctRecipientIds.Count > MessageTexts.MaximumRecipients ? MessageOutcome.TooManyRecipients
        : null;
}
