using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.CardTemplates;

/// <param name="Version">The template version the upload is based on; a stale version is rejected.</param>
/// <param name="Content">The Typst template file (<c>.typ</c>, UTF-8) as base64.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CardTemplateUploadRequest(
    [Range(0, long.MaxValue)] long Version,
    [Required] byte[] Content);
