using System.Text.Json;
using SciFiLarpNetwork.Backend.Domain.Events;

namespace SciFiLarpNetwork.Backend.Tests.Domain;

public sealed class DomainEventTests
{
    [Fact]
    public void ClonesPayloadAndAcceptsValidUtcMetadata()
    {
        using var document = JsonDocument.Parse("{\"value\":\"original\"}");
        var sourcePayload = document.RootElement;
        var domainEvent = new DomainEvent(
            Guid.NewGuid(),
            "network.created",
            1,
            DateTimeOffset.UtcNow,
            sourcePayload);

        Assert.Equal("original", domainEvent.Payload.GetProperty("value").GetString());
    }

    [Theory]
    [InlineData("empty-id", typeof(ArgumentException))]
    [InlineData("blank-event-type", typeof(ArgumentException))]
    [InlineData("zero-version", typeof(ArgumentOutOfRangeException))]
    public void RejectsInvalidEventIdentityAndVersion(string scenario, Type expectedExceptionType)
    {
        var payload = JsonSerializer.SerializeToElement(new { value = "test" });

        var exception = Record.Exception(() =>
        {
            _ = scenario switch
            {
                "empty-id" => new DomainEvent(Guid.Empty, "network.created", 1, DateTimeOffset.UtcNow, payload),
                "blank-event-type" => new DomainEvent(Guid.NewGuid(), " ", 1, DateTimeOffset.UtcNow, payload),
                "zero-version" => new DomainEvent(Guid.NewGuid(), "network.created", 0, DateTimeOffset.UtcNow, payload),
                _ => throw new ArgumentOutOfRangeException(nameof(scenario))
            };
        });

        Assert.Equal(expectedExceptionType, exception?.GetType());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RejectsNonUtcOrUndefinedPayload(bool undefinedPayload)
    {
        var payload = JsonSerializer.SerializeToElement(new { value = "test" });

        var exception = Record.Exception(() => new DomainEvent(
            Guid.NewGuid(),
            "network.created",
            1,
            undefinedPayload
                ? DateTimeOffset.UtcNow
                : new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)),
            undefinedPayload ? default : payload));

        Assert.Equal(typeof(ArgumentException), exception?.GetType());
    }
}
