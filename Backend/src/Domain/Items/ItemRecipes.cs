using System.Globalization;

namespace SciFiLarpNetwork.Backend.Domain.Items;

/// <summary>
/// Rules for an item's components. Their order does not matter, so two recipes are the same when they hold the same
/// components with the same quantities.
/// </summary>
public static class ItemRecipes
{
    public const int MaximumQuantity = 99;

    public const int MaximumComponents = 20;

    /// <summary>Checks one recipe on its own, without looking at other items.</summary>
    public static ItemRecipeProblem FindStructuralProblem(Guid itemId, IReadOnlyList<ItemComponent> components) =>
        components.Any(component => component.Quantity is < 1 or > MaximumQuantity) ? ItemRecipeProblem.InvalidQuantity
        : components.Any(component => component.ItemId == itemId) ? ItemRecipeProblem.SelfComponent
        : components.DistinctBy(component => component.ItemId).Count() != components.Count ? ItemRecipeProblem.DuplicateComponent
        : components.Count > MaximumComponents ? ItemRecipeProblem.TooManyComponents
        : ItemRecipeProblem.None;

    /// <summary>
    /// The text two items share exactly when their recipes are the same. An item without components gets a key of its
    /// own, because only non-empty recipes must be unique.
    /// </summary>
    public static string Key(Guid itemId, IReadOnlyList<ItemComponent> components) =>
        components.Count == 0
            ? $"none:{itemId:N}"
            : string.Join(',', components
                .OrderBy(component => component.ItemId)
                .Select(component => string.Create(CultureInfo.InvariantCulture, $"{component.ItemId:N}x{component.Quantity}")));

    /// <summary>
    /// Whether giving <paramref name="itemId"/> these components would make it contain itself through them.
    /// <paramref name="componentsOf"/> returns the stored components of any other item.
    /// </summary>
    public static bool CreatesLoop(
        Guid itemId,
        IEnumerable<ItemComponent> components,
        Func<Guid, IReadOnlyList<ItemComponent>> componentsOf)
    {
        var visited = new HashSet<Guid>();
        var pending = new Stack<Guid>(components.Select(component => component.ItemId));
        while (pending.TryPop(out var current))
        {
            if (current == itemId)
            {
                return true;
            }

            if (!visited.Add(current))
            {
                continue;
            }

            foreach (var component in componentsOf(current))
            {
                pending.Push(component.ItemId);
            }
        }

        return false;
    }
}
