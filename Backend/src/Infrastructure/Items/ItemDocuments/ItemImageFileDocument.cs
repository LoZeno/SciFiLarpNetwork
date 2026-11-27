namespace SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;

/// <summary>
/// The recorded facts of an uploaded item picture, whose bytes are in the media folder. It commits with the event that
/// sets the picture, so the image route can find the type of any picture, current or replaced. A clone shares its
/// source's picture. Documents are written once and never changed or deleted.
/// </summary>
public sealed record ItemImageFileDocument(Guid Id, Guid ItemId, string MediaType, int ByteCount);
