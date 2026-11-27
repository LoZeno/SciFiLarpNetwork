using SciFiLarpNetwork.Backend.Domain.Items;

namespace SciFiLarpNetwork.Backend.Tests.Domain.Items;

public sealed class RarityOrderTests
{
    private static readonly Guid Common = Guid.NewGuid();
    private static readonly Guid Uncommon = Guid.NewGuid();
    private static readonly Guid Rare = Guid.NewGuid();
    private static readonly Guid[] Order = [Common, Uncommon, Rare];

    private static bool NoneArchived(Guid rarityId) => false;

    private static bool UncommonArchived(Guid rarityId) => rarityId == Uncommon;

    [Fact]
    public void MovingUpSwapsWithTheRarityAbove()
    {
        Assert.Equal([Common, Rare, Uncommon], RarityOrder.Move(Order, Rare, up: true, NoneArchived));
    }

    [Fact]
    public void MovingDownSwapsWithTheRarityBelow()
    {
        Assert.Equal([Uncommon, Common, Rare], RarityOrder.Move(Order, Common, up: false, NoneArchived));
    }

    [Fact]
    public void MovingTheFirstRarityUpChangesNothing()
    {
        Assert.Equal(Order, RarityOrder.Move(Order, Common, up: true, NoneArchived));
    }

    [Fact]
    public void MovingTheLastRarityDownChangesNothing()
    {
        Assert.Equal(Order, RarityOrder.Move(Order, Rare, up: false, NoneArchived));
    }

    [Fact]
    public void MovingAnUnknownRarityChangesNothing()
    {
        Assert.Equal(Order, RarityOrder.Move(Order, Guid.NewGuid(), up: true, NoneArchived));
    }

    [Fact]
    public void MovingUpSkipsAnArchivedRarity()
    {
        Assert.Equal([Rare, Uncommon, Common], RarityOrder.Move(Order, Rare, up: true, UncommonArchived));
    }

    [Fact]
    public void MovingDownSkipsAnArchivedRarity()
    {
        Assert.Equal([Rare, Uncommon, Common], RarityOrder.Move(Order, Common, up: false, UncommonArchived));
    }

    [Fact]
    public void MovingUpPastOnlyArchivedRaritiesChangesNothing()
    {
        Assert.Equal(Order, RarityOrder.Move(Order, Uncommon, up: true, rarityId => rarityId == Common));
    }
}
