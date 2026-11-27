using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Api.Networks;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.Items;

[ApiController]
[Route("api/v1/items")]
[Produces(SirenMediaTypes.Json)]
public sealed class ItemsController(
    IItemService itemService,
    IItemImageService imageService,
    IItemCopyService copyService,
    IApplicationText text) : ControllerBase
{
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet]
    public async Task<ActionResult<SirenEntity<ItemCollectionProperties>>> GetItems(
        [FromQuery] string? search,
        [FromQuery] Guid? itemTypeId,
        [FromQuery] bool includeArchived,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var query = new ItemQuery(search, itemTypeId, includeArchived, page);
        return Ok(Resources.ItemCollection(await itemService.GetItemsAsync(query, cancellationToken), query));
    }

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{itemId:guid}")]
    public async Task<ActionResult<SirenEntity<ItemSummary>>> GetItem(Guid itemId, CancellationToken cancellationToken) =>
        Ok(Resources.Item(await itemService.GetItemAsync(itemId, cancellationToken)
                          ?? throw ItemErrors.ToApiException(ItemOutcome.NotFound)));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{itemId:guid}/copies")]
    public async Task<ActionResult<SirenEntity<ItemCopyCollectionProperties>>> GetCopies(Guid itemId, CancellationToken cancellationToken) =>
        Ok(Resources.CopyCollection(itemId, await copyService.GetCopiesAsync(itemId, cancellationToken)
                                            ?? throw ItemErrors.ToApiException(ItemOutcome.NotFound)));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost]
    public async Task<ActionResult<SirenEntity<ItemSummary>>> CreateItem(
        [FromBody] ItemCreateRequest request,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created,
            Resources.Item(ItemErrors.Unwrap(await itemService.CreateItemAsync(request.ToCreation(), cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPut("{itemId:guid}")]
    public async Task<ActionResult<SirenEntity<ItemSummary>>> UpdateItem(
        Guid itemId,
        [FromBody] ItemUpdateRequest request,
        CancellationToken cancellationToken) =>
        Ok(Resources.Item(ItemErrors.Unwrap(
            await itemService.UpdateItemAsync(itemId, request.Version, request.ToDefinition(), cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{itemId:guid}/archive")]
    public async Task<ActionResult<SirenEntity<ItemSummary>>> ArchiveItem(Guid itemId, CancellationToken cancellationToken) =>
        Ok(Resources.Item(ItemErrors.Unwrap(await itemService.ArchiveItemAsync(itemId, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{itemId:guid}/unarchive")]
    public async Task<ActionResult<SirenEntity<ItemSummary>>> UnarchiveItem(Guid itemId, CancellationToken cancellationToken) =>
        Ok(Resources.Item(ItemErrors.Unwrap(await itemService.UnarchiveItemAsync(itemId, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{itemId:guid}/components")]
    public async Task<ActionResult<SirenEntity<ItemSummary>>> AddComponent(
        Guid itemId,
        [FromBody] ItemComponentAddRequest request,
        CancellationToken cancellationToken) =>
        Ok(Resources.Item(ItemErrors.Unwrap(await itemService.AddComponentAsync(
            itemId, request.Version, request.ItemId, request.Quantity, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPut("{itemId:guid}/components/{componentItemId:guid}")]
    public async Task<ActionResult<SirenEntity<ItemSummary>>> ChangeComponentQuantity(
        Guid itemId,
        Guid componentItemId,
        [FromBody] ItemComponentQuantityRequest request,
        CancellationToken cancellationToken) =>
        Ok(Resources.Item(ItemErrors.Unwrap(await itemService.ChangeComponentQuantityAsync(
            itemId, request.Version, componentItemId, request.Quantity, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpDelete("{itemId:guid}/components/{componentItemId:guid}")]
    public async Task<ActionResult<SirenEntity<ItemSummary>>> RemoveComponent(
        Guid itemId,
        Guid componentItemId,
        [FromBody] ItemVersionRequest request,
        CancellationToken cancellationToken) =>
        Ok(Resources.Item(ItemErrors.Unwrap(
            await itemService.RemoveComponentAsync(itemId, request.Version, componentItemId, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPut("{itemId:guid}/image")]
    [RequestSizeLimit(NetworkLimits.MaximumImageRequestBytes)]
    public async Task<ActionResult<SirenEntity<ItemSummary>>> SetImage(
        Guid itemId,
        [FromBody] ItemImageRequest request,
        CancellationToken cancellationToken) =>
        Ok(Resources.Item(ItemErrors.Unwrap(
            await imageService.SetImageAsync(itemId, request.Version, request.Content, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpDelete("{itemId:guid}/image")]
    public async Task<ActionResult<SirenEntity<ItemSummary>>> RemoveImage(
        Guid itemId,
        [FromBody] ItemVersionRequest request,
        CancellationToken cancellationToken) =>
        Ok(Resources.Item(ItemErrors.Unwrap(await imageService.RemoveImageAsync(itemId, request.Version, cancellationToken))));

    private ItemResources Resources => new(text, Request.PathBase, User.IsInRole(AdministratorRoles.Administrator));
}
