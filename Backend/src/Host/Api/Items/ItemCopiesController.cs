using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.Items;

[ApiController]
[Route("api/v1/item-copies")]
[Produces(SirenMediaTypes.Json)]
public sealed class ItemCopiesController(IItemCopyService copyService, IApplicationText text) : ControllerBase
{
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{copyId:guid}")]
    public async Task<ActionResult<SirenEntity<ItemCopySummary>>> GetCopy(Guid copyId, CancellationToken cancellationToken) =>
        Ok(Resources.Copy(await copyService.GetCopyAsync(copyId, cancellationToken)
                          ?? throw ItemErrors.ToApiException(ItemOutcome.NotFound)));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{copyId:guid}/deactivate")]
    public async Task<ActionResult<SirenEntity<ItemCopySummary>>> DeactivateCopy(Guid copyId, CancellationToken cancellationToken) =>
        Ok(Resources.Copy(ItemErrors.Unwrap(await copyService.DeactivateCopyAsync(copyId, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{copyId:guid}/reactivate")]
    public async Task<ActionResult<SirenEntity<ItemCopySummary>>> ReactivateCopy(Guid copyId, CancellationToken cancellationToken) =>
        Ok(Resources.Copy(ItemErrors.Unwrap(await copyService.ReactivateCopyAsync(copyId, cancellationToken))));

    private ItemResources Resources => new(text, Request.PathBase, User.IsInRole(AdministratorRoles.Administrator));
}
