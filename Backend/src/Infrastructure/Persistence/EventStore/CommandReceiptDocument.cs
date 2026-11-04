namespace SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

public sealed record CommandReceiptDocument(
    Guid Id,
    Guid StreamId,
    long StreamVersion,
    string RequestHash,
    IReadOnlyList<Guid> EventIds);
