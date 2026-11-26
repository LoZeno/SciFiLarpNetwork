namespace SciFiLarpNetwork.Management.Api.Items;

internal sealed record ItemCollectionProperties(
    string Title,
    string Description,
    string? Search,
    Guid? ItemTypeId,
    bool IncludesArchived,
    int PageNumber,
    int PageCount,
    long TotalCount,
    int MaximumNameLength,
    int MaximumTextLength,
    int MaximumCost,
    int MaximumCharges,
    int MaximumQuantity,
    int MaximumComponents,
    int MaximumCopiesPerItem,
    int MaximumCards,
    int MaximumImageByteCount);
