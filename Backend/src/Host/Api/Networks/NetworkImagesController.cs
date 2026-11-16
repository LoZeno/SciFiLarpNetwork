using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.Networks;

namespace SciFiLarpNetwork.Backend.Api.Networks;

/// <summary>
/// Serves stored network images. An image id always names the same bytes, so responses may be cached for long.
/// </summary>
[ApiController]
[Route("api/v1/network-images")]
public sealed class NetworkImagesController(INetworkBackgroundImageService imageService) : ControllerBase
{
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{imageId:guid}")]
    public async Task<IActionResult> GetImage(Guid imageId, CancellationToken cancellationToken)
    {
        var image = await imageService.GetImageAsync(imageId, cancellationToken)
            ?? throw NetworkErrors.ToApiException(NetworkOutcome.NotFound);
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = "private, max-age=31536000, immutable";
        return File(image.Content, image.MediaType);
    }
}
