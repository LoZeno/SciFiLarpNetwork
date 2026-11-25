using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.News;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.News;

[ApiController]
[Route("api/v1/news")]
[Produces(SirenMediaTypes.Json)]
public sealed class NewsController(INewsStoryService stories, IApplicationText text) : ControllerBase
{
    /// <summary>
    /// Lists one network's stories, latest start first; <paramref name="activeAt"/> keeps those active at that instant.
    /// Without <paramref name="networkId"/> the list is empty.
    /// </summary>
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet]
    public async Task<ActionResult<SirenEntity<NewsCollectionProperties>>> GetStories(
        [FromQuery] Guid? networkId,
        [FromQuery] DateTimeOffset? activeAt,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var query = new NewsStoryQuery(networkId, activeAt, page);
        return Ok(Resources.NewsCollection(await stories.GetStoriesAsync(query, cancellationToken), query));
    }

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{storyId:guid}")]
    public async Task<ActionResult<SirenEntity<NewsStoryDetails>>> GetStory(Guid storyId, CancellationToken cancellationToken) =>
        Ok(Resources.Story(await stories.GetStoryAsync(storyId, cancellationToken)
            ?? throw NewsErrors.ToApiException(NewsOutcome.NotFound)));

    /// <summary>Creates a story. A repeated submission returns the story already created.</summary>
    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost]
    public async Task<ActionResult<SirenEntity<NewsStoryDetails>>> CreateStory(
        [FromBody] NewsStoryCreateRequest request,
        CancellationToken cancellationToken)
    {
        var created = NewsErrors.Unwrap(
            await stories.CreateStoryAsync(request.StoryId, request.ToContent(), cancellationToken));
        return StatusCode(StatusCodes.Status201Created, Resources.Story(created));
    }

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPut("{storyId:guid}")]
    public async Task<ActionResult<SirenEntity<NewsStoryDetails>>> EditStory(
        Guid storyId,
        [FromBody] NewsStoryEditRequest request,
        CancellationToken cancellationToken) =>
        Ok(Resources.Story(NewsErrors.Unwrap(
            await stories.EditStoryAsync(storyId, request.Version, request.ToContent(), cancellationToken))));

    private NewsResources Resources =>
        new(text, Request.PathBase, User.IsInRole(AdministratorRoles.Administrator));
}
