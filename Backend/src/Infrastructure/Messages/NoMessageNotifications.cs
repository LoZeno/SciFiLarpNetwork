using SciFiLarpNetwork.Backend.Application.Messages;

namespace SciFiLarpNetwork.Backend.Infrastructure.Messages;

/// <summary>Used when the host publishes no live updates, for example outside the web host.</summary>
internal sealed class NoMessageNotifications : IMessageNotifications
{
    public Task MessageSentAsync(MessageSentNotice notice, CancellationToken cancellationToken) => Task.CompletedTask;
}
