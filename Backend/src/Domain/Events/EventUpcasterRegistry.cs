using System.Text.Json;

namespace SciFiLarpNetwork.Backend.Domain.Events;

public sealed class EventUpcasterRegistry(IEnumerable<IEventUpcaster> upcasters)
{
    private readonly Dictionary<(string EventType, int FromVersion), IEventUpcaster> _upcasters =
        CreateRegistry(upcasters);

    public (int SchemaVersion, JsonElement Payload) Upcast(
        string eventType,
        int schemaVersion,
        JsonElement payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentOutOfRangeException.ThrowIfLessThan(schemaVersion, 1);

        var currentVersion = schemaVersion;
        var currentPayload = payload.Clone();
        while (_upcasters.TryGetValue((eventType, currentVersion), out var upcaster))
        {
            currentPayload = upcaster.Upcast(currentPayload).Clone();
            currentVersion = upcaster.ToVersion;
        }

        return (currentVersion, currentPayload);
    }

    private static Dictionary<(string EventType, int FromVersion), IEventUpcaster> CreateRegistry(
        IEnumerable<IEventUpcaster> upcasters)
    {
        var result = new Dictionary<(string EventType, int FromVersion), IEventUpcaster>();
        foreach (var upcaster in upcasters)
        {
            if (string.IsNullOrWhiteSpace(upcaster.EventType) ||
                upcaster.FromVersion < 1 ||
                upcaster.ToVersion != upcaster.FromVersion + 1)
            {
                throw new ArgumentException("Upcasters must declare an event type and advance one positive schema version.");
            }

            if (!result.TryAdd((upcaster.EventType, upcaster.FromVersion), upcaster))
            {
                throw new ArgumentException(
                    $"More than one upcaster is registered for '{upcaster.EventType}' version {upcaster.FromVersion}.");
            }
        }

        return result;
    }
}
