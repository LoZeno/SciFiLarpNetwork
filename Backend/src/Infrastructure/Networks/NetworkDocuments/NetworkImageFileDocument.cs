namespace SciFiLarpNetwork.Backend.Infrastructure.Networks.NetworkDocuments;

/// <summary>
/// The recorded facts of an uploaded network image, whose bytes are in the media folder. It commits with the event that
/// sets the image, so the image route can find the type of any image, current or replaced. Documents are written once
/// and never changed or deleted.
/// </summary>
public sealed record NetworkImageFileDocument(Guid Id, Guid NetworkId, string MediaType, int ByteCount);
