using System.Text.Json;
using System.Text.Json.Nodes;
using SciFiLarpNetwork.Backend.Domain.Events;

namespace SciFiLarpNetwork.Backend.Tests.EventSourcing;

public sealed class EventUpcasterRegistryTests
{
    [Fact]
    public void UpcastsDeterministicallyAcrossConsecutiveVersions()
    {
        var registry = new EventUpcasterRegistry(
        [
            new RenameNamePropertyUpcaster(1, 2),
            new AddStatusPropertyUpcaster(2, 3)
        ]);
        var oldPayload = JsonSerializer.SerializeToElement(new { name = "Test" });

        var first = registry.Upcast("character.registered", 1, oldPayload);
        var second = registry.Upcast("character.registered", 1, oldPayload);

        Assert.Equal(3, first.SchemaVersion);
        Assert.Equal(first.SchemaVersion, second.SchemaVersion);
        Assert.Equal(first.Payload.GetRawText(), second.Payload.GetRawText());
        Assert.Equal("Test", first.Payload.GetProperty("displayName").GetString());
        Assert.Equal("active", first.Payload.GetProperty("status").GetString());
        Assert.False(first.Payload.TryGetProperty("name", out _));
    }

    [Fact]
    public void RejectsDuplicateUpcastersForSameEventVersion()
    {
        Assert.Throws<ArgumentException>(
            () => new EventUpcasterRegistry(
            [
                new RenameNamePropertyUpcaster(1, 2),
                new RenameNamePropertyUpcaster(1, 2)
            ]));
    }

    private sealed class RenameNamePropertyUpcaster(int fromVersion, int toVersion) : IEventUpcaster
    {
        public string EventType => "character.registered";

        public int FromVersion => fromVersion;

        public int ToVersion => toVersion;

        public JsonElement Upcast(JsonElement payload)
        {
            var value = JsonNode.Parse(payload.GetRawText())?.AsObject() ??
                        throw new InvalidOperationException("The test payload must be an object.");
            value["displayName"] = value["name"]?.DeepClone();
            value.Remove("name");
            return JsonSerializer.SerializeToElement(value);
        }
    }

    private sealed class AddStatusPropertyUpcaster(int fromVersion, int toVersion) : IEventUpcaster
    {
        public string EventType => "character.registered";

        public int FromVersion => fromVersion;

        public int ToVersion => toVersion;

        public JsonElement Upcast(JsonElement payload)
        {
            var value = JsonNode.Parse(payload.GetRawText())?.AsObject() ??
                        throw new InvalidOperationException("The test payload must be an object.");
            value["status"] = "active";
            return JsonSerializer.SerializeToElement(value);
        }
    }
}
