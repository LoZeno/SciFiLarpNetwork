using SciFiLarpNetwork.Backend.Domain.Items;

namespace SciFiLarpNetwork.Backend.Domain.Events.ItemEvents;

/// <summary>Records the item's whole new list of components after an addition, a quantity change or a removal.</summary>
public sealed record ItemComponentsChanged(
    Guid ItemId,
    ItemComponent[] Components,
    string RecipeKey,
    DateTimeOffset OccurredAtUtc);
