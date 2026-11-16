using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SciFiLarpNetwork.Backend.Api.Networks;

/// <param name="Version">The network version the change is based on; a stale version is rejected.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record NetworkVersionRequest([Range(1, long.MaxValue)] long Version);
