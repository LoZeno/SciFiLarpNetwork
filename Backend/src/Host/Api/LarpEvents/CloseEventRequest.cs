using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Backend.Application.LarpEvents;

namespace SciFiLarpNetwork.Backend.Api.LarpEvents;

/// <param name="Version">The event version the closing grid was built from; a stale version is rejected.</param>
/// <param name="Awards">The experience each participant gets; participants left out get nothing.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CloseEventRequest(
    [Range(1, long.MaxValue)] long Version,
    [property: JsonRequired] IReadOnlyList<ClosingAwardRequest> Awards)
{
    public IReadOnlyList<ClosingAward> ToAwards() =>
        Awards.Select(award => new ClosingAward(award.CharacterId, award.BaseExperience, award.DayIds)).ToArray();
}
