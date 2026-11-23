using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using Marten.Subscriptions;
using Microsoft.Extensions.Logging;
using SciFiLarpNetwork.Backend.Application.Messages;
using SciFiLarpNetwork.Backend.Domain.Events.MessageEvents;

namespace SciFiLarpNetwork.Backend.Infrastructure.Messages;

/// <summary>
/// Publishes every committed <see cref="MessageSent"/> to live clients. It runs in the async daemon after the send's
/// transaction, which also wrote the inline <c>MessageDocument</c>, so a client that reloads on the notice always finds
/// the message. It starts from the present: messages sent before the host started are not announced again.
/// </summary>
internal sealed partial class MessageSentSubscription : SubscriptionBase
{
    private readonly IMessageNotifications _notifications;
    private readonly ILogger<MessageSentSubscription> _logger;

    public MessageSentSubscription(IMessageNotifications notifications, ILogger<MessageSentSubscription> logger)
    {
        _notifications = notifications;
        _logger = logger;
        Name = "MessageNotifications";
        IncludeType<MessageSent>();
        Options.SubscribeFromPresent();
    }

    public override async Task<IChangeListener> ProcessEventsAsync(
        EventRange page,
        ISubscriptionController controller,
        IDocumentOperations operations,
        CancellationToken cancellationToken)
    {
        foreach (var stored in page.Events)
        {
            if (stored.Data is not MessageSent sent)
            {
                continue;
            }

            try
            {
                await _notifications.MessageSentAsync(new MessageSentNotice(sent.MessageId, sent.RecipientIds), cancellationToken);
            }
            catch (Exception failure) when (failure is not OperationCanceledException)
            {
                // A live update is only a hint to reload; a failed one must not stop the daemon or repeat the message.
                LogPublishFailed(failure, sent.MessageId);
            }
        }

        return NullChangeListener.Instance;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not publish the live update for message {MessageId}.")]
    private partial void LogPublishFailed(Exception exception, Guid messageId);
}
