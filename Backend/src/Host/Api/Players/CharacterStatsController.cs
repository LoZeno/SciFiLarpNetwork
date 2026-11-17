using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.Players;

[ApiController]
[Route("api/v1/character-stats")]
[Produces(SirenMediaTypes.Json)]
public sealed class CharacterStatsController(ICharacterStatService statService, IApplicationText text) : ControllerBase
{
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet]
    public async Task<ActionResult<SirenEntity<CharacterStatCollectionProperties>>> GetStats(
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken) =>
        Ok(Resources.StatCollection(await statService.GetStatsAsync(includeArchived, cancellationToken), includeArchived));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{statId:guid}")]
    public async Task<ActionResult<SirenEntity<CharacterStatSummary>>> GetStat(Guid statId,
        CancellationToken cancellationToken) =>
        Ok(Resources.Stat(await statService.GetStatAsync(statId, cancellationToken)
                          ?? throw RosterErrors.ToApiException(RosterOutcome.NotFound)));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPut("{statId:guid}")]
    public async Task<ActionResult<SirenEntity<CharacterStatSummary>>> RenameStat(
        Guid statId,
        [FromBody] CharacterStatRenameRequest request,
        CancellationToken cancellationToken) =>
        Ok(Resources.Stat(RosterErrors.Unwrap(
            await statService.RenameStatAsync(statId, request.Version, request.Name, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{statId:guid}/archive")]
    public async Task<ActionResult<SirenEntity<CharacterStatSummary>>> ArchiveStat(Guid statId,
        CancellationToken cancellationToken) =>
        Ok(Resources.Stat(RosterErrors.Unwrap(await statService.ArchiveStatAsync(statId, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{statId:guid}/unarchive")]
    public async Task<ActionResult<SirenEntity<CharacterStatSummary>>> UnarchiveStat(Guid statId,
        CancellationToken cancellationToken) =>
        Ok(Resources.Stat(RosterErrors.Unwrap(await statService.UnarchiveStatAsync(statId, cancellationToken))));

    private RosterResources Resources =>
        new(text, Request.PathBase, User.IsInRole(AdministratorRoles.Administrator));
}
