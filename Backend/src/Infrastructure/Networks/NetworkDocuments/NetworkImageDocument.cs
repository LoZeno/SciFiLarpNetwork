namespace SciFiLarpNetwork.Backend.Infrastructure.Networks.NetworkDocuments;

/// <summary>
/// An uploaded network image, stored as its own document so image bytes never enter the event stream.
/// Documents are written once and never changed or deleted.
/// </summary>
public sealed record NetworkImageDocument(Guid Id, Guid NetworkId, string MediaType, byte[] Content);
