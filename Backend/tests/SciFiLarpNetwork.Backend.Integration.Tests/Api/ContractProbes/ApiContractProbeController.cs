using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Api;
using SciFiLarpNetwork.Backend.Application.EventSourcing;
using SciFiLarpNetwork.Backend.Domain.Events;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Api.ContractProbes;

[ApiController]
[Route("api/v1/contract-test")]
[Produces(SirenMediaTypes.Json)]
public sealed class ApiContractProbeController(IEventStreamStore eventStreamStore) : ControllerBase
{
    [HttpGet("message-action")]
    public ActionResult<SirenEntity<object>> GetMessageAction() =>
        Ok(new SirenEntity<object>
        {
            Class = ["message-form"],
            Actions =
            [
                new SirenAction
                {
                    Name = "submit-message",
                    Href = $"{Request.PathBase}{Request.Path}",
                    Method = "POST",
                    Title = "Submit message",
                    Type = "application/json",
                    Fields = [new SirenField { Name = "name", Title = "Name" }]
                }
            ]
        });

    [HttpPost("message-action")]
    public ActionResult<SirenEntity<ProbeRequest>> SubmitMessage([FromBody] ProbeRequest request) =>
        Ok(new SirenEntity<ProbeRequest>
        {
            Class = ["message-submitted"],
            Properties = request
        });

    [HttpPost("validation")]
    public IActionResult ValidateRequest([FromBody] ProbeRequest request) =>
        Ok(new { request.Name });

    [HttpPost("conflict")]
    public IActionResult ThrowConflict()
    {
        if (!HttpContext.Request.Path.HasValue)
        {
            return NotFound();
        }

        throw new ApiException(
            StatusCodes.Status409Conflict,
            ApiErrorCodes.Conflict,
            ApiErrorCodes.Conflict);
    }

    [HttpGet("unexpected-error")]
    public IActionResult UnexpectedError()
    {
        if (!HttpContext.Request.Path.HasValue)
        {
            return NotFound();
        }

        throw new InvalidOperationException("This message must not reach the client.");
    }

    [Authorize]
    [HttpPost("protected-action")]
    public IActionResult ProtectedAction() => Ok();

    [Authorize(Policy = "ContractAdmin")]
    [HttpPost("admin-action")]
    public IActionResult AdminAction() => Ok();

    [Authorize]
    [HttpPost("append-event")]
    public async Task<ActionResult<SirenEntity<object>>> AppendEvent(
        [FromBody] ProbeEventAppendRequest request,
        CancellationToken cancellationToken)
    {
        var domainEvent = new DomainEvent(
            Guid.NewGuid(),
            "contract-test.actor-attributed",
            1,
            DateTimeOffset.UtcNow,
            JsonSerializer.SerializeToElement(new { accepted = true }),
            request.ActorId);
        await eventStreamStore.AppendAsync(request.StreamId, 0, [domainEvent], cancellationToken: cancellationToken);
        var storedEvent = Assert.Single(await eventStreamStore.ReadStreamAsync(request.StreamId, cancellationToken));
        return Ok(new SirenEntity<object>
        {
            Class = ["actor-attributed-event"],
            Properties = new { storedEvent.ActorId }
        });
    }
}

public sealed record ProbeEventAppendRequest(Guid StreamId, string? ActorId);
