using Microsoft.AspNetCore.SignalR;
using SciFiLarpNetwork.Backend.Application.Messages;

namespace SciFiLarpNetwork.Backend.Api.Messages;

/// <summary>Sends each notice to every connection of <see cref="MessageHub"/>, which only readers and administrators can open.</summary>
public sealed class SignalRMessageNotifications(IHubContext<MessageHub> hub) : IMessageNotifications
{
    public Task MessageSentAsync(MessageSentNotice notice, CancellationToken cancellationToken) =>
        hub.Clients.All.SendAsync(MessageHub.MessageSentMethod, notice, cancellationToken);
}
