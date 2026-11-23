namespace SciFiLarpNetwork.Management.Api.Messages;

/// <summary>What a screen does with the live message updates.</summary>
/// <param name="OnMessageSent">Runs for each sent message.</param>
/// <param name="OnReconnected">Runs after a lost connection comes back; notices sent meanwhile are lost.</param>
/// <param name="OnConnectionLost">
/// Runs when the connection is lost: with <see langword="true"/> while it is still trying to reconnect, and with
/// <see langword="false"/> when it has stopped for good, for example because the session is no longer valid.
/// </param>
public sealed record MessageLiveHandlers(
    Func<MessageSentNotice, Task> OnMessageSent,
    Func<Task> OnReconnected,
    Func<bool, Task> OnConnectionLost);
