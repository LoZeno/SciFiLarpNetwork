namespace SciFiLarpNetwork.Management.Api.Items;

internal sealed record ItemProperties(
    Guid ItemId,
    string Name,
    Guid ItemTypeId,
    Guid RarityId,
    string? Description,
    string? Effect,
    int Cost,
    int? Charges,
    DateOnly? ExpiresOn,
    Guid? ImageId,
    IReadOnlyList<ItemComponentProperties> Components,
    bool IsArchived,
    long Version);
