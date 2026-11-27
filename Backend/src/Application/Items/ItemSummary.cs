namespace SciFiLarpNetwork.Backend.Application.Items;

/// <param name="ImageId">The item's current picture, or <see langword="null"/> when it has none.</param>
/// <param name="Components">The components in name order.</param>
public sealed record ItemSummary(
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
    IReadOnlyList<ItemComponentSummary> Components,
    bool IsArchived,
    long Version);
