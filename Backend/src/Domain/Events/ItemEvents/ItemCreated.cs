using SciFiLarpNetwork.Backend.Domain.Items;

namespace SciFiLarpNetwork.Backend.Domain.Events.ItemEvents;

/// <param name="RecipeKey">The <see cref="ItemRecipes.Key"/> of <paramref name="Components"/>.</param>
/// <param name="ClonedFromItemId">The item this one was cloned from, if any.</param>
public sealed record ItemCreated(
    Guid ItemId,
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
    Guid? ClonedFromItemId,
    DateTimeOffset OccurredAtUtc);
