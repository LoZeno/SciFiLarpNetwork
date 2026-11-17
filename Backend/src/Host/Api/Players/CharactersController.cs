using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.Players;

[ApiController]
[Route("api/v1/characters")]
[Produces(SirenMediaTypes.Json)]
public sealed class CharactersController(ICharacterService characterService, IApplicationText text) : ControllerBase
{
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet]
    public async Task<ActionResult<SirenEntity<RosterCollectionProperties>>> SearchCharacters(
        [FromQuery] string? search,
        [FromQuery] bool includeArchived,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var query = new RosterQuery(search, includeArchived, page);
        return Ok(Resources.CharacterCollection(await characterService.SearchCharactersAsync(query, cancellationToken), query));
    }

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("by-qr")]
    public async Task<ActionResult<SirenEntity<CharacterSummary>>> FindByQrIdentifier(
        [FromQuery] string? identifier,
        CancellationToken cancellationToken) =>
        Ok(Resources.Character(RosterErrors.Unwrap(
            await characterService.FindByQrIdentifierAsync(identifier ?? string.Empty, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{characterId:guid}")]
    public async Task<ActionResult<SirenEntity<CharacterSummary>>> GetCharacter(Guid characterId,
        CancellationToken cancellationToken) =>
        Ok(Resources.Character(await characterService.GetCharacterAsync(characterId, cancellationToken)
                               ?? throw RosterErrors.ToApiException(RosterOutcome.NotFound)));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPut("{characterId:guid}")]
    public async Task<ActionResult<SirenEntity<CharacterSummary>>> UpdateCharacter(
        Guid characterId,
        [FromBody] CharacterUpdateRequest request,
        CancellationToken cancellationToken) =>
        Ok(Resources.Character(RosterErrors.Unwrap(await characterService.UpdateCharacterAsync(
            characterId, request.Version, request.ToDefinition(), cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{characterId:guid}/regenerate-qr-identifier")]
    public async Task<ActionResult<SirenEntity<CharacterSummary>>> RegenerateQrIdentifier(Guid characterId,
        CancellationToken cancellationToken) =>
        Ok(Resources.Character(RosterErrors.Unwrap(
            await characterService.RegenerateQrIdentifierAsync(characterId, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{characterId:guid}/archive")]
    public async Task<ActionResult<SirenEntity<CharacterSummary>>> ArchiveCharacter(Guid characterId,
        CancellationToken cancellationToken) =>
        Ok(Resources.Character(RosterErrors.Unwrap(await characterService.ArchiveCharacterAsync(characterId, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{characterId:guid}/unarchive")]
    public async Task<ActionResult<SirenEntity<CharacterSummary>>> UnarchiveCharacter(Guid characterId,
        CancellationToken cancellationToken) =>
        Ok(Resources.Character(RosterErrors.Unwrap(await characterService.UnarchiveCharacterAsync(characterId, cancellationToken))));

    private RosterResources Resources =>
        new(text, Request.PathBase, User.IsInRole(AdministratorRoles.Administrator));
}
