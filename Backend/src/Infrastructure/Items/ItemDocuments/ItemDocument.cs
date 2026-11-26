using SciFiLarpNetwork.Backend.Domain.Items;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;

/// <param name="RecipeKey">The <see cref="ItemRecipes.Key"/> of the components; unique, so no two items share a recipe.</param>
/// <param name="SearchText">The normalized name, description and effect, searched by the item list.</param>
/// <param name="ImageId">The current picture, or <see langword="null"/> when the item has none.</param>
public sealed record ItemDocument(
    Guid Id,
    string Name,
    string NormalizedName,
    Guid ItemTypeId,
    Guid RarityId,
    string? Description,
    string? Effect,
    int Cost,
    int? Charges,
    DateOnly? ExpiresOn,
    ItemComponent[] Components,
    string RecipeKey,
    string SearchText,
    Guid? ImageId,
    bool IsArchived)
{
    /// <summary>The item stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
