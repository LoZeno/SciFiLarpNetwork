using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.Skills;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.Skills;

[ApiController]
[Route("api/v1/skill-card-flags")]
[Produces(SirenMediaTypes.Json)]
public sealed class CardFlagsController(ICardFlagService cardFlagService, IApplicationText text) : ControllerBase
{
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet]
    public async Task<ActionResult<SirenEntity<CardFlagCollectionProperties>>> GetCardFlags(CancellationToken cancellationToken) =>
        Ok(new SkillCatalogResources(text, Request.PathBase, User.IsInRole(AdministratorRoles.Administrator))
            .CardFlagCollection(await cardFlagService.GetCardFlagsAsync(cancellationToken)));
}
