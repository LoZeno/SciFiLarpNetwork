using SciFiLarpNetwork.Backend.Domain.Items;

namespace SciFiLarpNetwork.Backend.Application.Items;

/// <param name="ClonedFromItemId">The item being cloned; the new item then also gets its current image.</param>
public sealed record ItemCreation(
    ItemDefinition Definition,
    IReadOnlyList<ItemComponent> Components,
    Guid? ClonedFromItemId);
