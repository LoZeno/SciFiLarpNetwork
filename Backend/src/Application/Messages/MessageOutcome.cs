namespace SciFiLarpNetwork.Backend.Application.Messages;

public enum MessageOutcome
{
    Succeeded,
    NotFound,

    /// <summary>The client must give the message an identifier that is not empty.</summary>
    InvalidMessageId,

    InvalidTitle,
    InvalidBody,
    NoRecipients,

    /// <summary>A message holds at most 200 recipients.</summary>
    TooManyRecipients,

    /// <summary>The sender or recipients listed in <see cref="MessageResult{T}.CharacterIds"/> do not exist.</summary>
    UnknownCharacters,

    /// <summary>The sender or recipients listed in <see cref="MessageResult{T}.CharacterIds"/> are archived.</summary>
    ArchivedCharacters,

    /// <summary>The message a reply answers does not exist.</summary>
    UnknownReplySource,

    /// <summary>The identifier already names a different message, or another stream.</summary>
    MessageIdInUse
}
