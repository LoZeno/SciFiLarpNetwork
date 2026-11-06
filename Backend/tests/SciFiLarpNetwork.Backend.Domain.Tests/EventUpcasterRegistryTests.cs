using System.Text.Json;
using System.Text.Json.Nodes;
using SciFiLarpNetwork.Backend.Domain.Events;

namespace SciFiLarpNetwork.Backend.Tests.Domain;

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

        Assert.Equal(
            new { SchemaVersion = 3, Payload = """{"displayName":"Test","status":"active"}""" },
            new { first.SchemaVersion, Payload = first.Payload.GetRawText() });
    }

    [Fact]
    public void RepeatedUpcastingProducesTheSameVersionAndPayload()
    {
        var registry = new EventUpcasterRegistry(
        [
            new RenameNamePropertyUpcaster(1, 2),
            new AddStatusPropertyUpcaster(2, 3)
        ]);
        var oldPayload = JsonSerializer.SerializeToElement(new { name = "Test" });

        var first = registry.Upcast("character.registered", 1, oldPayload);
        var second = registry.Upcast("character.registered", 1, oldPayload);

        Assert.Equal(
            new { first.SchemaVersion, Payload = first.Payload.GetRawText() },
            new { second.SchemaVersion, Payload = second.Payload.GetRawText() });
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
