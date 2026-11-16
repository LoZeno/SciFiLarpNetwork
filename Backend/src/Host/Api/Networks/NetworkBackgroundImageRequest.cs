using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.Networks;

/// <param name="Version">The network version the upload is based on; a stale version is rejected.</param>
/// <param name="Content">The image file as base64. The server detects the format from the bytes.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record NetworkBackgroundImageRequest(
    [Range(1, long.MaxValue)] long Version,
    [Required] byte[] Content);
