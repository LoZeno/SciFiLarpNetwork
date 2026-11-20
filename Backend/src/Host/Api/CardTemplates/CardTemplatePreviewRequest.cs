using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.CardTemplates;

/// <param name="Content">The Typst template file (<c>.typ</c>, UTF-8) as base64. It is not saved.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CardTemplatePreviewRequest([Required] byte[] Content);
