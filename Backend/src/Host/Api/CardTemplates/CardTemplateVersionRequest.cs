using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.CardTemplates;

/// <param name="Version">The template version the change is based on; a stale version is rejected.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CardTemplateVersionRequest([Range(0, long.MaxValue)] long Version);
