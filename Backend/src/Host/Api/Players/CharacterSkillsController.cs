using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.Players;

[ApiController]
[Route("api/v1/characters/{characterId:guid}/skills")]
[Produces(SirenMediaTypes.Json)]
public sealed class CharacterSkillsController(ICharacterSkillService skillService, IApplicationText text) : ControllerBase
{
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet]
    public async Task<ActionResult<SirenEntity<CharacterSkillsSummary>>> GetCharacterSkills(Guid characterId,
        CancellationToken cancellationToken) =>
        Ok(Resources.CharacterSkills(await skillService.GetCharacterSkillsAsync(characterId, cancellationToken)
                                     ?? throw RosterErrors.ToApiException(RosterOutcome.NotFound)));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPut]
    public async Task<ActionResult<SirenEntity<CharacterSkillsSummary>>> SaveCharacterSkills(
        Guid characterId,
        [FromBody] CharacterSkillsRequest request,
        CancellationToken cancellationToken) =>
        Ok(Resources.CharacterSkills(RosterErrors.Unwrap(await skillService.SaveCharacterSkillsAsync(
            characterId, request.Version, request.ToDefinition(), cancellationToken))));

    private RosterResources Resources =>
        new(text, Request.PathBase, User.IsInRole(AdministratorRoles.Administrator));
}
