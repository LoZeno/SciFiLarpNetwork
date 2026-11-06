namespace SciFiLarpNetwork.Backend.Application.EventSourcing;

public interface IEventActorContext
{
    bool RequiresAuthenticatedActor { get; }

    string? ActorId { get; }
}