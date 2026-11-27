using SciFiLarpNetwork.Backend.Domain.Items;

namespace SciFiLarpNetwork.Backend.Tests.Domain.Items;

public sealed class ItemRecipesTests
{
    private static readonly Guid Item = Guid.NewGuid();
    private static readonly Guid PowerCell = Guid.NewGuid();
    private static readonly Guid Casing = Guid.NewGuid();

    [Fact]
    public void ValidRecipeHasNoProblem()
    {
        Assert.Equal(ItemRecipeProblem.None, ItemRecipes.FindStructuralProblem(Item, [new(PowerCell, 2), new(Casing, 1)]));
    }

    [Fact]
    public void EmptyRecipeHasNoProblem()
    {
        Assert.Equal(ItemRecipeProblem.None, ItemRecipes.FindStructuralProblem(Item, []));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(ItemRecipes.MaximumQuantity + 1)]
    public void QuantityOutsideTheRangeIsInvalid(int quantity)
    {
        Assert.Equal(ItemRecipeProblem.InvalidQuantity, ItemRecipes.FindStructuralProblem(Item, [new(PowerCell, quantity)]));
    }

    [Fact]
    public void MaximumQuantityIsValid()
    {
        Assert.Equal(ItemRecipeProblem.None, ItemRecipes.FindStructuralProblem(Item, [new(PowerCell, ItemRecipes.MaximumQuantity)]));
    }

    [Fact]
    public void ItemCannotBeItsOwnComponent()
    {
        Assert.Equal(ItemRecipeProblem.SelfComponent, ItemRecipes.FindStructuralProblem(Item, [new(Item, 1)]));
    }

    [Fact]
    public void SameComponentTwiceIsADuplicate()
    {
        Assert.Equal(ItemRecipeProblem.DuplicateComponent, ItemRecipes.FindStructuralProblem(Item, [new(PowerCell, 1), new(PowerCell, 2)]));
    }

    [Fact]
    public void MoreThanTheMaximumComponentsIsTooMany()
    {
        ItemComponent[] components = [.. Enumerable.Range(0, ItemRecipes.MaximumComponents + 1).Select(_ => new ItemComponent(Guid.NewGuid(), 1))];

        Assert.Equal(ItemRecipeProblem.TooManyComponents, ItemRecipes.FindStructuralProblem(Item, components));
    }

    [Fact]
    public void MaximumComponentsAreAllowed()
    {
        ItemComponent[] components = [.. Enumerable.Range(0, ItemRecipes.MaximumComponents).Select(_ => new ItemComponent(Guid.NewGuid(), 1))];

        Assert.Equal(ItemRecipeProblem.None, ItemRecipes.FindStructuralProblem(Item, components));
    }

    [Fact]
    public void RecipeKeyIgnoresComponentOrder()
    {
        Assert.Equal(
            ItemRecipes.Key(Item, [new(PowerCell, 2), new(Casing, 1)]),
            ItemRecipes.Key(Guid.NewGuid(), [new(Casing, 1), new(PowerCell, 2)]));
    }

    [Fact]
    public void RecipeKeyDependsOnQuantities()
    {
        Assert.NotEqual(ItemRecipes.Key(Item, [new(PowerCell, 2)]), ItemRecipes.Key(Item, [new(PowerCell, 3)]));
    }

    [Fact]
    public void EmptyRecipesOfDifferentItemsHaveDifferentKeys()
    {
        Assert.NotEqual(ItemRecipes.Key(Item, []), ItemRecipes.Key(PowerCell, []));
    }

    [Fact]
    public void ComponentContainingTheItemCreatesALoop()
    {
        var components = new Dictionary<Guid, IReadOnlyList<ItemComponent>> { [PowerCell] = [new(Casing, 1)], [Casing] = [new(Item, 1)] };

        Assert.True(ItemRecipes.CreatesLoop(Item, [new(PowerCell, 1)], id => components.GetValueOrDefault(id, [])));
    }

    [Fact]
    public void ComponentsSharingASubcomponentCreateNoLoop()
    {
        var components = new Dictionary<Guid, IReadOnlyList<ItemComponent>> { [PowerCell] = [new(Casing, 1)], [Casing] = [] };

        Assert.False(ItemRecipes.CreatesLoop(Item, [new(PowerCell, 1), new(Casing, 1)], id => components.GetValueOrDefault(id, [])));
    }

    [Fact]
    public void ALoopNotThroughTheItemIsNotReported()
    {
        var components = new Dictionary<Guid, IReadOnlyList<ItemComponent>> { [PowerCell] = [new(Casing, 1)], [Casing] = [new(PowerCell, 1)] };

        Assert.False(ItemRecipes.CreatesLoop(Item, [new(PowerCell, 1)], id => components.GetValueOrDefault(id, [])));
    }
}
