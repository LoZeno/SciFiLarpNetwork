using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Api.CharacterCards;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.CardTemplates;
using SciFiLarpNetwork.Backend.Application.CharacterCards;
using SciFiLarpNetwork.Backend.Application.LarpEvents;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.LarpEvents;

[ApiController]
[Route("api/v1/events")]
[Produces(SirenMediaTypes.Json)]
public sealed class LarpEventsController(
    ILarpEventService events,
    ILarpEventDayService days,
    ILarpEventParticipantService participants,
    ILarpEventCardService eventCards,
    ICharacterCardRenderer renderer,
    ICharacterCardTemplateService templates,
    IApplicationText text) : ControllerBase
{
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet]
    public async Task<ActionResult<SirenEntity<LarpEventCollectionProperties>>> GetEvents(
        [FromQuery] bool includeArchived,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var query = new LarpEventQuery(includeArchived, page);
        return Ok(Resources.EventCollection(await events.GetEventsAsync(query, cancellationToken), query));
    }

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{eventId:guid}")]
    public async Task<ActionResult<SirenEntity<LarpEventSummary>>> GetEvent(Guid eventId,
        CancellationToken cancellationToken) =>
        Ok(Resources.Event(await events.GetEventAsync(eventId, cancellationToken)
                           ?? throw LarpEventErrors.ToApiException(LarpEventOutcome.NotFound)));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{eventId:guid}/participants")]
    public async Task<ActionResult<SirenEntity<ParticipantCollectionProperties>>> GetParticipants(
        Guid eventId,
        [FromQuery] Guid? dayId,
        CancellationToken cancellationToken) =>
        Ok(Resources.ParticipantCollection(
            eventId,
            dayId,
            LarpEventErrors.Unwrap(await participants.GetParticipantsAsync(eventId, dayId, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost]
    public async Task<ActionResult<SirenEntity<LarpEventSummary>>> CreateEvent(
        [FromBody] LarpEventCreateRequest request,
        CancellationToken cancellationToken)
    {
        var created = LarpEventErrors.Unwrap(await events.CreateEventAsync(request.ToDefinition(), cancellationToken));
        return StatusCode(StatusCodes.Status201Created, Resources.Event(created));
    }

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPut("{eventId:guid}")]
    public async Task<ActionResult<SirenEntity<LarpEventSummary>>> UpdateEvent(
        Guid eventId,
        [FromBody] LarpEventUpdateRequest request,
        CancellationToken cancellationToken) =>
        EventResult(await events.UpdateEventAsync(eventId, request.Version, request.ToDefinition(), cancellationToken));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{eventId:guid}/close")]
    public async Task<ActionResult<SirenEntity<LarpEventSummary>>> CloseEvent(
        Guid eventId,
        [FromBody] CloseEventRequest request,
        CancellationToken cancellationToken) =>
        EventResult(await events.CloseEventAsync(eventId, request.Version, request.ToAwards(), cancellationToken));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{eventId:guid}/reopen")]
    public async Task<ActionResult<SirenEntity<LarpEventSummary>>> ReopenEvent(Guid eventId,
        CancellationToken cancellationToken) =>
        EventResult(await events.ReopenEventAsync(eventId, cancellationToken));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{eventId:guid}/archive")]
    public async Task<ActionResult<SirenEntity<LarpEventSummary>>> ArchiveEvent(Guid eventId,
        CancellationToken cancellationToken) =>
        EventResult(await events.ArchiveEventAsync(eventId, cancellationToken));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{eventId:guid}/unarchive")]
    public async Task<ActionResult<SirenEntity<LarpEventSummary>>> UnarchiveEvent(Guid eventId,
        CancellationToken cancellationToken) =>
        EventResult(await events.UnarchiveEventAsync(eventId, cancellationToken));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{eventId:guid}/days")]
    public async Task<ActionResult<SirenEntity<LarpEventSummary>>> AddDay(
        Guid eventId,
        [FromBody] DayAddRequest request,
        CancellationToken cancellationToken) =>
        EventResult(await days.AddDayAsync(eventId, request.Date, request.Experience, cancellationToken));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{eventId:guid}/days/update")]
    public async Task<ActionResult<SirenEntity<LarpEventSummary>>> UpdateDay(
        Guid eventId,
        [FromBody] DayUpdateRequest request,
        CancellationToken cancellationToken) =>
        EventResult(await days.UpdateDayAsync(eventId, request.DayId, request.Date, request.Experience, cancellationToken));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{eventId:guid}/days/remove")]
    public async Task<ActionResult<SirenEntity<LarpEventSummary>>> RemoveDay(
        Guid eventId,
        [FromBody] DayRemoveRequest request,
        CancellationToken cancellationToken) =>
        EventResult(await days.RemoveDayAsync(eventId, request.DayId, cancellationToken));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{eventId:guid}/participants")]
    public async Task<ActionResult<SirenEntity<LarpEventSummary>>> RegisterParticipant(
        Guid eventId,
        [FromBody] ParticipantDaysRequest request,
        CancellationToken cancellationToken)
    {
        LarpEventErrors.Unwrap(await participants.RegisterAsync(eventId, request.CharacterId, request.DayIds, cancellationToken));
        return await CurrentEventAsync(eventId, cancellationToken);
    }

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{eventId:guid}/participants/unregister")]
    public async Task<ActionResult<SirenEntity<LarpEventSummary>>> UnregisterParticipant(
        Guid eventId,
        [FromBody] ParticipantDaysRequest request,
        CancellationToken cancellationToken) =>
        EventResult(await participants.UnregisterAsync(eventId, request.CharacterId, request.DayIds, cancellationToken));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{eventId:guid}/participants/experience")]
    public async Task<ActionResult<SirenEntity<LarpEventSummary>>> ChangeExperience(
        Guid eventId,
        [FromBody] ExperienceChangeRequest request,
        CancellationToken cancellationToken)
    {
        LarpEventErrors.Unwrap(await participants.ChangeExperienceAsync(eventId, request.CharacterId, request.Amount, cancellationToken));
        return await CurrentEventAsync(eventId, cancellationToken);
    }

    /// <summary>Prints the cards of an event's participants. Printing changes nothing, so readers can print too.</summary>
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpPost("{eventId:guid}/cards")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, CharacterCardsController.PdfMediaType)]
    public async Task<IActionResult> PrintEventCards(
        Guid eventId,
        [FromBody] LarpEventCardsRequest request,
        CancellationToken cancellationToken)
    {
        var cards = LarpEventErrors.Unwrap(await eventCards.GetCardsAsync(eventId, request.DayId, cancellationToken));
        var document = new CharacterCardDocument(
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
            CharacterCardLabels.Localize(text),
            cards);
        try
        {
            var template = await templates.GetSourceAsync(cancellationToken);
            return File(await renderer.RenderAsync(document, template, cancellationToken),
                CharacterCardsController.PdfMediaType, CharacterCardsController.FileName);
        }
        catch (CardRenderingException)
        {
            throw CharacterCardErrors.RenderingFailed();
        }
    }

    private async Task<ActionResult<SirenEntity<LarpEventSummary>>> CurrentEventAsync(Guid eventId,
        CancellationToken cancellationToken) =>
        Ok(Resources.Event(await events.GetEventAsync(eventId, cancellationToken)
                           ?? throw LarpEventErrors.ToApiException(LarpEventOutcome.NotFound)));

    private OkObjectResult EventResult(LarpEventResult<LarpEventSummary> result) =>
        Ok(Resources.Event(LarpEventErrors.Unwrap(result)));

    private LarpEventResources Resources =>
        new(text, Request.PathBase, User.IsInRole(AdministratorRoles.Administrator));
}
