using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.News;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.News;

[ApiController]
[Route("api/v1/news-media")]
public sealed class NewsMediaController(INewsMediaService media, IApplicationText text) : ControllerBase
{
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet]
    [Produces(SirenMediaTypes.Json)]
    public async Task<ActionResult<SirenEntity<NewsMediaCollectionProperties>>> GetMedia(CancellationToken cancellationToken) =>
        Ok(Resources.MediaCollection(await media.GetMediaAsync(cancellationToken)));

    /// <summary>Uploads a picture or video as <c>multipart/form-data</c> with a <c>name</c> and a <c>file</c>.</summary>
    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost]
    [Produces(SirenMediaTypes.Json)]
    [RequestSizeLimit(NewsLimits.MaximumUploadRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = NewsLimits.MaximumUploadRequestBytes)]
    public async Task<ActionResult<SirenEntity<NewsMediaSummary>>> UploadMedia(
        [FromForm] string? name,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        var uploaded = NewsErrors.Unwrap(await media.UploadMediaAsync(name, content, cancellationToken));
        return StatusCode(StatusCodes.Status201Created, Resources.MediaItem(uploaded));
    }

    /// <summary>Removes media that no story uses.</summary>
    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpDelete("{mediaId:guid}")]
    [Produces(SirenMediaTypes.Json)]
    public async Task<IActionResult> RemoveMedia(Guid mediaId, CancellationToken cancellationToken)
    {
        var outcome = await media.RemoveMediaAsync(mediaId, cancellationToken);
        return outcome == NewsOutcome.Succeeded ? NoContent() : throw NewsErrors.ToApiException(outcome);
    }

    /// <summary>
    /// Serves the stored file with its stored type and supports range requests, so a video can jump ahead. An
    /// identifier always names the same bytes, so responses may be cached for long.
    /// </summary>
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{mediaId:guid}/content")]
    public async Task<IActionResult> GetContent(Guid mediaId, CancellationToken cancellationToken)
    {
        var stored = await media.OpenMediaAsync(mediaId, cancellationToken)
            ?? throw NewsErrors.ToApiException(NewsOutcome.NotFound);
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = "private, max-age=31536000, immutable";
        return File(stored.Content, stored.MediaType, enableRangeProcessing: true);
    }

    private NewsResources Resources =>
        new(text, Request.PathBase, User.IsInRole(AdministratorRoles.Administrator));
}
