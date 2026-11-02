using System.Text.Json;
using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api;

public static class SirenJson
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Task SerializeAsync<T>(Stream stream, T value, CancellationToken cancellationToken) =>
        JsonSerializer.SerializeAsync(stream, value, SerializerOptions, cancellationToken);

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, SerializerOptions);
}
