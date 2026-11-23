namespace SciFiLarpNetwork.Backend.Application.Messages;

/// <summary>Publishes sent messages to live clients. Delivery is best effort: a client reloads after reconnecting.</summary>
public interface IMessageNotifications
{
    Task MessageSentAsync(MessageSentNotice notice, CancellationToken cancellationToken);
}
