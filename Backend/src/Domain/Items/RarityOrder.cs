namespace SciFiLarpNetwork.Backend.Domain.Items;

/// <summary>The administrator-set order of rarities, for example Common before Rare.</summary>
public static class RarityOrder
{
    /// <summary>
    /// Swaps <paramref name="rarityId"/> with its neighbour above (<paramref name="up"/>) or below. A rarity already at
    /// that end, or not in the order, leaves the order unchanged.
    /// </summary>
    public static IReadOnlyList<Guid> Move(IReadOnlyList<Guid> order, Guid rarityId, bool up)
    {
        var index = order.ToList().IndexOf(rarityId);
        var target = up ? index - 1 : index + 1;
        if (index < 0 || target < 0 || target >= order.Count)
        {
            return order;
        }

        var moved = order.ToArray();
        (moved[index], moved[target]) = (moved[target], moved[index]);
        return moved;
    }
}
