using System.Security.Claims;
using SciFiLarpNetwork.Backend.Application.EventSourcing;

namespace SciFiLarpNetwork.Backend.Api;

public sealed class HttpEventActorContext(IHttpContextAccessor httpContextAccessor) : IEventActorContext
{
    public bool RequiresAuthenticatedActor => httpContextAccessor.HttpContext is not null;

    public string? ActorId => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true
        ? httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
        : null;
}