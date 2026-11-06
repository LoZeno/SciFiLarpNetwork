using System.Text.Json;

namespace SciFiLarpNetwork.Backend.Domain.Events;

public interface IEventUpcaster
{
    string EventType { get; }

    int FromVersion { get; }

    int ToVersion { get; }

    JsonElement Upcast(JsonElement payload);
}
