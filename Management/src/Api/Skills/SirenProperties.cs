using System.Text.Json;

namespace SciFiLarpNetwork.Management.Api.Skills;

internal static class SirenProperties
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static T Read<T>(JsonElement? properties) =>
        properties is { ValueKind: JsonValueKind.Object } value
            ? value.Deserialize<T>(JsonOptions) ?? throw new JsonException("The API returned empty properties.")
            : throw new JsonException("The API returned a resource without properties.");
}
