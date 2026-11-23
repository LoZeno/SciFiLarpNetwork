namespace SciFiLarpNetwork.Backend.Application.Messages;

/// <summary>Messages between characters, as administrators inspect and send them.</summary>
public interface IMessageService
{
    /// <summary>Lists messages without their bodies, newest first.</summary>
    Task<MessagePage> GetMessagesAsync(MessageQuery query, CancellationToken cancellationToken = default);

    /// <summary>Returns the message with its body and records that the current account opened it.</summary>
    Task<MessageResult<MessageDetails>> OpenMessageAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message. Sending the same <see cref="MessageDraft.MessageId"/> again with the same content returns the
    /// message already sent and sends nothing new; with different content it reports
    /// <see cref="MessageOutcome.MessageIdInUse"/>.
    /// </summary>
    Task<MessageResult<MessageDetails>> SendMessageAsync(MessageDraft draft, CancellationToken cancellationToken = default);
}
