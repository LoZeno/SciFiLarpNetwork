using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SciFiLarpNetwork.Backend.Api.Messages;

/// <summary>
/// Live updates of the message list for readers and administrators. Clients only listen: the hub sends
/// <see cref="MessageSentMethod"/> with a <c>MessageSentNotice</c> and offers no methods to call. A connection closes when
/// its administrator session expires.
/// </summary>
[Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
public sealed class MessageHub : Hub
{
    /// <summary>The hub's address below the application's path base.</summary>
    public const string Path = "/api/v1/messages/live";

    /// <summary>The client method that receives each sent message.</summary>
    public const string MessageSentMethod = "messageSent";
}
