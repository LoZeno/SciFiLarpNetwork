namespace SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

public sealed record GlobalEventIdDocument(Guid Id, Guid StreamId);
