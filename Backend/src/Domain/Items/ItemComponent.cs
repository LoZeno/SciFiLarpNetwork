namespace SciFiLarpNetwork.Backend.Domain.Items;

/// <summary>One component of an item: another item, and how many of it the recipe needs.</summary>
public sealed record ItemComponent(Guid ItemId, int Quantity);
