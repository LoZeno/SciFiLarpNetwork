namespace SciFiLarpNetwork.Backend.Domain.Items;

/// <summary>The administrator-set order of rarities, for example Common before Rare.</summary>
public static class RarityOrder
{
    /// <summary>
    /// Swaps <paramref name="rarityId"/> with the nearest rarity above (<paramref name="up"/>) or below that is not
    /// archived; archived rarities in between keep their places. Lists hide archived rarities by default, so swapping
    /// with one would look like nothing happened. A rarity with no such neighbour, or not in the order, leaves the order
    /// unchanged.
    /// </summary>
    public static IReadOnlyList<Guid> Move(IReadOnlyList<Guid> order, Guid rarityId, bool up, Func<Guid, bool> isArchived)
    {
        var index = order.ToList().IndexOf(rarityId);
        if (index < 0)
        {
            return order;
        }

        var step = up ? -1 : 1;
        var target = index + step;
        while (target >= 0 && target < order.Count && isArchived(order[target]))
        {
            target += step;
        }

        if (target < 0 || target >= order.Count)
        {
            return order;
        }

        var moved = order.ToArray();
        (moved[index], moved[target]) = (moved[target], moved[index]);
        return moved;
    }
}
