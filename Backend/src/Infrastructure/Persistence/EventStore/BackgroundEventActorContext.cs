using SciFiLarpNetwork.Backend.Application.EventSourcing;

namespace SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

internal sealed class BackgroundEventActorContext : IEventActorContext
{
    public bool RequiresAuthenticatedActor => false;

    public string? ActorId => null;
}
