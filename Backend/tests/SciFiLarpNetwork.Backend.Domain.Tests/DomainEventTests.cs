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

    [Fact]
    public void RejectsInvalidEventIdentityAndVersion()
    {
        var payload = JsonSerializer.SerializeToElement(new { value = "test" });

        Assert.Throws<ArgumentException>(() =>
            new DomainEvent(Guid.Empty, "network.created", 1, DateTimeOffset.UtcNow, payload));
        Assert.Throws<ArgumentException>(() =>
            new DomainEvent(Guid.NewGuid(), " ", 1, DateTimeOffset.UtcNow, payload));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DomainEvent(Guid.NewGuid(), "network.created", 0, DateTimeOffset.UtcNow, payload));
    }

    [Fact]
    public void RejectsNonUtcOrUndefinedPayload()
    {
        var payload = JsonSerializer.SerializeToElement(new { value = "test" });

        Assert.Throws<ArgumentException>(() =>
            new DomainEvent(
                Guid.NewGuid(),
                "network.created",
                1,
                new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)),
                payload));
        Assert.Throws<ArgumentException>(() =>
            new DomainEvent(
                Guid.NewGuid(),
                "network.created",
                1,
                DateTimeOffset.UtcNow,
                default));
    }
}
