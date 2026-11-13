using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.Skills;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.Skills;

[ApiController]
[Route("api/v1/skills")]
[Produces(SirenMediaTypes.Json)]
public sealed class SkillsController(ISkillService skillService, IApplicationText text) : ControllerBase
{
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet]
    public async Task<ActionResult<SirenEntity<SkillCollectionProperties>>> GetSkills(
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken) =>
        Ok(Resources.SkillCollection(
            await skillService.GetSkillsAsync(null, includeArchived, cancellationToken),
            includeArchived,
            Resources.SkillsPath));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{skillId:guid}")]
    public async Task<ActionResult<SirenEntity<SkillSummary>>> GetSkill(Guid skillId, CancellationToken cancellationToken) =>
        Ok(Resources.Skill(await skillService.GetSkillAsync(skillId, cancellationToken)
            ?? throw CatalogErrors.ToApiException(CatalogOutcome.NotFound)));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost]
    public async Task<ActionResult<SirenEntity<SkillSummary>>> CreateSkill(
        [FromBody] SkillCreateRequest request,
        CancellationToken cancellationToken)
    {
        var skill = CatalogErrors.Unwrap(await skillService.CreateSkillAsync(request.ToDefinition(), cancellationToken));
        return StatusCode(StatusCodes.Status201Created, Resources.Skill(skill));
    }

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPut("{skillId:guid}")]
    public async Task<ActionResult<SirenEntity<SkillSummary>>> UpdateSkill(
        Guid skillId,
        [FromBody] SkillUpdateRequest request,
        CancellationToken cancellationToken) =>
        Ok(Resources.Skill(CatalogErrors.Unwrap(
            await skillService.UpdateSkillAsync(skillId, request.Version, request.ToDefinition(), cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{skillId:guid}/archive")]
    public async Task<ActionResult<SirenEntity<SkillSummary>>> ArchiveSkill(Guid skillId, CancellationToken cancellationToken) =>
        Ok(Resources.Skill(CatalogErrors.Unwrap(await skillService.ArchiveSkillAsync(skillId, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{skillId:guid}/unarchive")]
    public async Task<ActionResult<SirenEntity<SkillSummary>>> UnarchiveSkill(Guid skillId, CancellationToken cancellationToken) =>
        Ok(Resources.Skill(CatalogErrors.Unwrap(await skillService.UnarchiveSkillAsync(skillId, cancellationToken))));

    private SkillCatalogResources Resources =>
        new(text, Request.PathBase, User.IsInRole(AdministratorRoles.Administrator));
}
