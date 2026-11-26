using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.Items;

namespace SciFiLarpNetwork.Backend.Api.Items;

/// <summary>Serves stored item pictures. An image id always names the same bytes, so responses may be cached for long.</summary>
[ApiController]
[Route("api/v1/item-images")]
public sealed class ItemImagesController(IItemImageService imageService) : ControllerBase
{
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{imageId:guid}")]
    public async Task<IActionResult> GetImage(Guid imageId, CancellationToken cancellationToken)
    {
        var image = await imageService.GetImageAsync(imageId, cancellationToken)
                    ?? throw ItemErrors.ToApiException(ItemOutcome.NotFound);
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = "private, max-age=31536000, immutable";
        return File(image.Content, image.MediaType);
    }
}
