using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.Messages;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.Messages;

[ApiController]
[Route("api/v1/messages")]
[Produces(SirenMediaTypes.Json)]
public sealed class MessagesController(IMessageService messages, IApplicationText text) : ControllerBase
{
    /// <summary>Lists messages without their bodies, newest first; <paramref name="characterId"/> keeps its incoming ones.</summary>
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet]
    public async Task<ActionResult<SirenEntity<MessageCollectionProperties>>> GetMessages(
        [FromQuery] Guid? characterId,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var query = new MessageQuery(characterId, page);
        return Ok(Resources.MessageCollection(await messages.GetMessagesAsync(query, cancellationToken), query));
    }

    /// <summary>Returns the message with its body; every opening is recorded with the signed-in account.</summary>
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{messageId:guid}")]
    public async Task<ActionResult<SirenEntity<MessageDetails>>> GetMessage(Guid messageId,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(Resources.Message(MessageErrors.Unwrap(await messages.OpenMessageAsync(messageId, cancellationToken))));
    }

    /// <summary>Sends a message or a reply. A repeated submission returns the message already sent.</summary>
    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost]
    public async Task<ActionResult<SirenEntity<MessageDetails>>> SendMessage(
        [FromBody] MessageSendRequest request,
        CancellationToken cancellationToken)
    {
        var sent = MessageErrors.Unwrap(await messages.SendMessageAsync(request.ToDraft(), cancellationToken));
        return StatusCode(StatusCodes.Status201Created, Resources.Message(sent));
    }

    private MessageResources Resources =>
        new(text, Request.PathBase, User.IsInRole(AdministratorRoles.Administrator));
}
