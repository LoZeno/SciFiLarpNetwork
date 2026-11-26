using SciFiLarpNetwork.Backend.Domain.Items;

namespace SciFiLarpNetwork.Backend.Tests.Domain.Items;

public sealed class RarityOrderTests
{
    private static readonly Guid Common = Guid.NewGuid();
    private static readonly Guid Uncommon = Guid.NewGuid();
    private static readonly Guid Rare = Guid.NewGuid();
    private static readonly Guid[] Order = [Common, Uncommon, Rare];

    [Fact]
    public void MovingUpSwapsWithTheRarityAbove()
    {
        Assert.Equal([Common, Rare, Uncommon], RarityOrder.Move(Order, Rare, up: true));
    }

    [Fact]
    public void MovingDownSwapsWithTheRarityBelow()
    {
        Assert.Equal([Uncommon, Common, Rare], RarityOrder.Move(Order, Common, up: false));
    }

    [Fact]
    public void MovingTheFirstRarityUpChangesNothing()
    {
        Assert.Equal(Order, RarityOrder.Move(Order, Common, up: true));
    }

    [Fact]
    public void MovingTheLastRarityDownChangesNothing()
    {
        Assert.Equal(Order, RarityOrder.Move(Order, Rare, up: false));
    }

    [Fact]
    public void MovingAnUnknownRarityChangesNothing()
    {
        Assert.Equal(Order, RarityOrder.Move(Order, Guid.NewGuid(), up: true));
    }
}
