using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.Items;

[ApiController]
[Route("api/v1/item-types")]
[Produces(SirenMediaTypes.Json)]
public sealed class ItemTypesController(IItemTypeService itemTypeService, IApplicationText text) : ControllerBase
{
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet]
    public async Task<ActionResult<SirenEntity<ItemCatalogCollectionProperties>>> GetItemTypes(
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken) =>
        Ok(Resources.ItemTypeCollection(await itemTypeService.GetItemTypesAsync(includeArchived, cancellationToken), includeArchived));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{itemTypeId:guid}")]
    public async Task<ActionResult<SirenEntity<ItemTypeSummary>>> GetItemType(Guid itemTypeId, CancellationToken cancellationToken) =>
        Ok(Resources.ItemType(await itemTypeService.GetItemTypeAsync(itemTypeId, cancellationToken)
                              ?? throw ItemErrors.ToApiException(ItemOutcome.NotFound)));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost]
    public async Task<ActionResult<SirenEntity<ItemTypeSummary>>> CreateItemType(
        [FromBody] ItemCatalogCreateRequest request,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created,
            Resources.ItemType(ItemErrors.Unwrap(await itemTypeService.CreateItemTypeAsync(request.Name, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPut("{itemTypeId:guid}")]
    public async Task<ActionResult<SirenEntity<ItemTypeSummary>>> RenameItemType(
        Guid itemTypeId,
        [FromBody] ItemCatalogRenameRequest request,
        CancellationToken cancellationToken) =>
        Ok(Resources.ItemType(ItemErrors.Unwrap(
            await itemTypeService.RenameItemTypeAsync(itemTypeId, request.Version, request.Name, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{itemTypeId:guid}/archive")]
    public async Task<ActionResult<SirenEntity<ItemTypeSummary>>> ArchiveItemType(Guid itemTypeId, CancellationToken cancellationToken) =>
        Ok(Resources.ItemType(ItemErrors.Unwrap(await itemTypeService.ArchiveItemTypeAsync(itemTypeId, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{itemTypeId:guid}/unarchive")]
    public async Task<ActionResult<SirenEntity<ItemTypeSummary>>> UnarchiveItemType(Guid itemTypeId, CancellationToken cancellationToken) =>
        Ok(Resources.ItemType(ItemErrors.Unwrap(await itemTypeService.UnarchiveItemTypeAsync(itemTypeId, cancellationToken))));

    private ItemResources Resources => new(text, Request.PathBase, User.IsInRole(AdministratorRoles.Administrator));
}
