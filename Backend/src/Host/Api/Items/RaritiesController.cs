using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.Items;

[ApiController]
[Route("api/v1/rarities")]
[Produces(SirenMediaTypes.Json)]
public sealed class RaritiesController(IRarityService rarityService, IApplicationText text) : ControllerBase
{
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet]
    public async Task<ActionResult<SirenEntity<ItemCatalogCollectionProperties>>> GetRarities(
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken) =>
        Ok(Resources.RarityCollection(await rarityService.GetRaritiesAsync(includeArchived, cancellationToken), includeArchived));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{rarityId:guid}")]
    public async Task<ActionResult<SirenEntity<RaritySummary>>> GetRarity(Guid rarityId, CancellationToken cancellationToken) =>
        Ok(Resources.Rarity(await rarityService.GetRarityAsync(rarityId, cancellationToken)
                            ?? throw ItemErrors.ToApiException(ItemOutcome.NotFound)));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost]
    public async Task<ActionResult<SirenEntity<RaritySummary>>> CreateRarity(
        [FromBody] ItemCatalogCreateRequest request,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created,
            Resources.Rarity(ItemErrors.Unwrap(await rarityService.CreateRarityAsync(request.Name, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPut("{rarityId:guid}")]
    public async Task<ActionResult<SirenEntity<RaritySummary>>> RenameRarity(
        Guid rarityId,
        [FromBody] ItemCatalogRenameRequest request,
        CancellationToken cancellationToken) =>
        Ok(Resources.Rarity(ItemErrors.Unwrap(
            await rarityService.RenameRarityAsync(rarityId, request.Version, request.Name, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{rarityId:guid}/move-up")]
    public async Task<ActionResult<SirenEntity<RaritySummary>>> MoveRarityUp(Guid rarityId, CancellationToken cancellationToken) =>
        Ok(Resources.Rarity(ItemErrors.Unwrap(await rarityService.MoveRarityAsync(rarityId, up: true, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{rarityId:guid}/move-down")]
    public async Task<ActionResult<SirenEntity<RaritySummary>>> MoveRarityDown(Guid rarityId, CancellationToken cancellationToken) =>
        Ok(Resources.Rarity(ItemErrors.Unwrap(await rarityService.MoveRarityAsync(rarityId, up: false, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{rarityId:guid}/archive")]
    public async Task<ActionResult<SirenEntity<RaritySummary>>> ArchiveRarity(Guid rarityId, CancellationToken cancellationToken) =>
        Ok(Resources.Rarity(ItemErrors.Unwrap(await rarityService.ArchiveRarityAsync(rarityId, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{rarityId:guid}/unarchive")]
    public async Task<ActionResult<SirenEntity<RaritySummary>>> UnarchiveRarity(Guid rarityId, CancellationToken cancellationToken) =>
        Ok(Resources.Rarity(ItemErrors.Unwrap(await rarityService.UnarchiveRarityAsync(rarityId, cancellationToken))));

    private ItemResources Resources => new(text, Request.PathBase, User.IsInRole(AdministratorRoles.Administrator));
}
