using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.Players;

[ApiController]
[Route("api/v1/players")]
[Produces(SirenMediaTypes.Json)]
public sealed class PlayersController(
    IPlayerService playerService,
    ICharacterService characterService,
    IApplicationText text) : ControllerBase
{
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet]
    public async Task<ActionResult<SirenEntity<RosterCollectionProperties>>> GetPlayers(
        [FromQuery] string? search,
        [FromQuery] bool includeArchived,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var query = new RosterQuery(search, includeArchived, page);
        return Ok(Resources.PlayerCollection(await playerService.GetPlayersAsync(query, cancellationToken), query));
    }

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{playerId:guid}")]
    public async Task<ActionResult<SirenEntity<PlayerSummary>>> GetPlayer(
        Guid playerId,
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken)
    {
        var player = await playerService.GetPlayerAsync(playerId, cancellationToken)
                     ?? throw RosterErrors.ToApiException(RosterOutcome.NotFound);
        return Ok(Resources.Player(player, includeArchived));
    }

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{playerId:guid}/characters")]
    public async Task<ActionResult<SirenEntity<RosterCollectionProperties>>> GetCharactersOfPlayer(
        Guid playerId,
        [FromQuery] bool includeArchived,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        CancellationToken cancellationToken = default)
    {
        _ = await playerService.GetPlayerAsync(playerId, cancellationToken)
            ?? throw RosterErrors.ToApiException(RosterOutcome.NotFound);
        var characters = await characterService.GetCharacterPageOfPlayerAsync(playerId, includeArchived, page, cancellationToken);
        return Ok(Resources.PlayerCharacterCollection(playerId, characters, new RosterQuery(null, includeArchived, page)));
    }

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost]
    public async Task<ActionResult<SirenEntity<PlayerSummary>>> CreatePlayer(
        [FromBody] PlayerCreateRequest request,
        CancellationToken cancellationToken)
    {
        var player = RosterErrors.Unwrap(await playerService.CreatePlayerAsync(request.ToDefinition(), cancellationToken));
        return StatusCode(StatusCodes.Status201Created, Resources.Player(player, includeArchivedCharacters: false));
    }

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPut("{playerId:guid}")]
    public async Task<ActionResult<SirenEntity<PlayerSummary>>> UpdatePlayer(
        Guid playerId,
        [FromBody] PlayerUpdateRequest request,
        CancellationToken cancellationToken) =>
        Ok(Resources.Player(
            RosterErrors.Unwrap(await playerService.UpdatePlayerAsync(playerId, request.Version, request.ToDefinition(),
                cancellationToken)),
            includeArchivedCharacters: false));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{playerId:guid}/deactivate")]
    public async Task<ActionResult<SirenEntity<PlayerSummary>>> DeactivatePlayer(Guid playerId,
        CancellationToken cancellationToken) =>
        Ok(Resources.Player(
            RosterErrors.Unwrap(await playerService.DeactivatePlayerAsync(playerId, cancellationToken)),
            includeArchivedCharacters: false));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{playerId:guid}/activate")]
    public async Task<ActionResult<SirenEntity<PlayerSummary>>> ActivatePlayer(Guid playerId,
        CancellationToken cancellationToken) =>
        Ok(Resources.Player(
            RosterErrors.Unwrap(await playerService.ActivatePlayerAsync(playerId, cancellationToken)),
            includeArchivedCharacters: false));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{playerId:guid}/archive")]
    public async Task<ActionResult<SirenEntity<PlayerSummary>>> ArchivePlayer(Guid playerId,
        CancellationToken cancellationToken) =>
        Ok(Resources.Player(
            RosterErrors.Unwrap(await playerService.ArchivePlayerAsync(playerId, cancellationToken)),
            includeArchivedCharacters: false));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{playerId:guid}/unarchive")]
    public async Task<ActionResult<SirenEntity<PlayerSummary>>> UnarchivePlayer(Guid playerId,
        CancellationToken cancellationToken) =>
        Ok(Resources.Player(
            RosterErrors.Unwrap(await playerService.UnarchivePlayerAsync(playerId, cancellationToken)),
            includeArchivedCharacters: false));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{playerId:guid}/characters")]
    public async Task<ActionResult<SirenEntity<CharacterSummary>>> CreateCharacter(
        Guid playerId,
        [FromBody] CharacterCreateRequest request,
        CancellationToken cancellationToken)
    {
        var character = RosterErrors.Unwrap(
            await characterService.CreateCharacterAsync(playerId, request.ToDefinition(), cancellationToken));
        return StatusCode(StatusCodes.Status201Created, Resources.Character(character));
    }

    private RosterResources Resources =>
        new(text, Request.PathBase, User.IsInRole(AdministratorRoles.Administrator));
}
