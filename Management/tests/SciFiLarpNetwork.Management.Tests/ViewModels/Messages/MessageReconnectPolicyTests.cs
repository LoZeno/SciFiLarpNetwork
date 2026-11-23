using System.Net;
using Microsoft.AspNetCore.SignalR.Client;
using SciFiLarpNetwork.Management.Api.Messages;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Messages;

public sealed class MessageReconnectPolicyTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 2)]
    [InlineData(2, 10)]
    [InlineData(3, 30)]
    [InlineData(500, 30)]
    public void RetriesForeverWithTheDelayCappedAtThirtySeconds(long previousRetries, int seconds) =>
        Assert.Equal(TimeSpan.FromSeconds(seconds), new MessageReconnectPolicy().NextRetryDelay(Context(previousRetries, new HttpRequestException("Offline."))));

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public void StopsWhenTheSessionIsRefused(HttpStatusCode status) =>
        Assert.Null(new MessageReconnectPolicy().NextRetryDelay(Context(1, new HttpRequestException("Refused.", null, status))));

    [Fact]
    public void KeepsTryingWhenTheBackendIsDown() =>
        Assert.NotNull(new MessageReconnectPolicy().NextRetryDelay(
            Context(10, new HttpRequestException("Unavailable.", null, HttpStatusCode.ServiceUnavailable))));

    private static RetryContext Context(long previousRetries, Exception reason) =>
        new() { PreviousRetryCount = previousRetries, ElapsedTime = TimeSpan.FromMinutes(1), RetryReason = reason };
}
