using System.Net;
using Microsoft.AspNetCore.SignalR.Client;

namespace SciFiLarpNetwork.Management.Api.Messages;

/// <summary>
/// Reconnects the live message updates for as long as the screen is open: at once, then after 2 and 10 seconds, then
/// every 30 seconds, so a backend restart or a gap in the venue network ends with the updates back. It stops only when
/// the backend refuses the session, which another try cannot fix.
/// </summary>
public sealed class MessageReconnectPolicy : IRetryPolicy
{
    private static readonly TimeSpan[] FirstDelays = [TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10)];
    private static readonly TimeSpan LaterDelay = TimeSpan.FromSeconds(30);

    public TimeSpan? NextRetryDelay(RetryContext retryContext) =>
        retryContext.RetryReason is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden }
            ? null
            : retryContext.PreviousRetryCount < FirstDelays.Length
                ? FirstDelays[retryContext.PreviousRetryCount]
                : LaterDelay;
}
