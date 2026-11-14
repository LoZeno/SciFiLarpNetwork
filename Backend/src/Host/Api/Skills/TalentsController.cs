using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.Skills;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.Skills;

[ApiController]
[Route("api/v1/talents")]
[Produces(SirenMediaTypes.Json)]
public sealed class TalentsController(
    ITalentService talentService,
    ISkillService skillService,
    IApplicationText text) : ControllerBase
{
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet]
    public async Task<ActionResult<SirenEntity<CatalogCollectionProperties>>> GetTalents(
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken) =>
        Ok(Resources.TalentCollection(await talentService.GetTalentsAsync(includeArchived, cancellationToken), includeArchived));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{talentId:guid}")]
    public async Task<ActionResult<SirenEntity<TalentSummary>>> GetTalent(Guid talentId, CancellationToken cancellationToken) =>
        Ok(Resources.Talent(await FindTalentAsync(talentId, cancellationToken)));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{talentId:guid}/skills")]
    public async Task<ActionResult<SirenEntity<SkillCollectionProperties>>> GetTalentSkills(
        Guid talentId,
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken)
    {
        await FindTalentAsync(talentId, cancellationToken);
        var skills = await skillService.GetSkillsAsync(talentId, includeArchived, cancellationToken);
        return Ok(Resources.SkillCollection(skills, includeArchived, Resources.TalentSkillsPath(talentId)));
    }

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost]
    public async Task<ActionResult<SirenEntity<TalentSummary>>> CreateTalent(
        [FromBody] TalentCreateRequest request,
        CancellationToken cancellationToken)
    {
        var talent = CatalogErrors.Unwrap(
            await talentService.CreateTalentAsync(new TalentDefinition(request.Name, request.Description), cancellationToken));
        return StatusCode(StatusCodes.Status201Created, Resources.Talent(talent));
    }

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPut("{talentId:guid}")]
    public async Task<ActionResult<SirenEntity<TalentSummary>>> UpdateTalent(
        Guid talentId,
        [FromBody] TalentUpdateRequest request,
        CancellationToken cancellationToken) =>
        Ok(Resources.Talent(CatalogErrors.Unwrap(await talentService.UpdateTalentAsync(
            talentId,
            request.Version,
            new TalentDefinition(request.Name, request.Description),
            cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{talentId:guid}/archive")]
    public async Task<ActionResult<SirenEntity<TalentSummary>>> ArchiveTalent(Guid talentId, CancellationToken cancellationToken) =>
        Ok(Resources.Talent(CatalogErrors.Unwrap(await talentService.ArchiveTalentAsync(talentId, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{talentId:guid}/unarchive")]
    public async Task<ActionResult<SirenEntity<TalentSummary>>> UnarchiveTalent(Guid talentId, CancellationToken cancellationToken) =>
        Ok(Resources.Talent(CatalogErrors.Unwrap(await talentService.UnarchiveTalentAsync(talentId, cancellationToken))));

    private SkillCatalogResources Resources =>
        new(text, Request.PathBase, User.IsInRole(AdministratorRoles.Administrator));

    private async Task<TalentSummary> FindTalentAsync(Guid talentId, CancellationToken cancellationToken) =>
        await talentService.GetTalentAsync(talentId, cancellationToken)
        ?? throw CatalogErrors.ToApiException(CatalogOutcome.NotFound);
}
