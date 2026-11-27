using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.Items;

/// <param name="Version">The item version the upload is based on; a stale version is rejected.</param>
/// <param name="Content">The image file as base64. The server detects the format from the bytes.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ItemImageRequest([Range(1, long.MaxValue)] long Version, [Required] byte[] Content);
