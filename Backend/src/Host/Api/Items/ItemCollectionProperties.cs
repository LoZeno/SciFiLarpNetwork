namespace SciFiLarpNetwork.Backend.Api.Items;

/// <summary>A page of items, with the limits the item dialogs and printing apply.</summary>
/// <param name="Search">The search text the page matches, or <see langword="null"/> for every item.</param>
/// <param name="ItemTypeId">The type the page is filtered by, or <see langword="null"/> for every type.</param>
public sealed record ItemCollectionProperties(
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
