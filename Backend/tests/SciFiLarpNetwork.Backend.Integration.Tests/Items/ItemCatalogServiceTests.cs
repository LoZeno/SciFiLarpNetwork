using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Items;

[Collection("Backend database integration")]
public sealed class ItemCatalogServiceTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private readonly ItemTestData data = new(factory);

    private static CancellationToken Token => ItemTestData.Token;

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CreatedItemTypeStartsAtVersionOne()
    {
        var result = await data.Types.CreateItemTypeAsync("  Weapon  ", Token);

        Assert.Equal(new { result.Outcome, Name = "Weapon", Version = 1L }, new { result.Outcome, result.Value!.Name, result.Value.Version });
    }

    [Fact]
    public async Task DuplicateItemTypeNameIsRejectedIgnoringCase()
    {
        await data.TypeAsync("Weapon");

        var result = await data.Types.CreateItemTypeAsync("WEAPON", Token);

        Assert.Equal(ItemOutcome.NameAlreadyUsed, result.Outcome);
    }

    [Fact]
    public async Task BlankItemTypeNameIsRejected()
    {
        var result = await data.Types.CreateItemTypeAsync("  ", Token);

        Assert.Equal(ItemOutcome.InvalidName, result.Outcome);
    }

    [Fact]
    public async Task RenamedItemTypeHasTheNewName()
    {
        var type = await data.TypeAsync("Weapon");

        var result = await data.Types.RenameItemTypeAsync(type.ItemTypeId, type.Version, "Weapons", Token);

        Assert.Equal(new { Name = "Weapons", Version = 2L }, new { result.Value!.Name, result.Value.Version });
    }

    [Fact]
    public async Task StaleItemTypeRenameIsRejected()
    {
        var type = await data.TypeAsync("Weapon");
        await data.Types.RenameItemTypeAsync(type.ItemTypeId, type.Version, "Weapons", Token);

        var result = await data.Types.RenameItemTypeAsync(type.ItemTypeId, type.Version, "Arms", Token);

        Assert.Equal(ItemOutcome.StaleVersion, result.Outcome);
    }

    [Fact]
    public async Task ArchivedItemTypesAreHiddenByDefault()
    {
        var weapon = await data.TypeAsync("Weapon");
        await data.TypeAsync("Tool");
        await data.Types.ArchiveItemTypeAsync(weapon.ItemTypeId, Token);

        var types = await data.Types.GetItemTypesAsync(includeArchived: false, Token);

        Assert.Equal(["Tool"], types.Select(type => type.Name));
    }

    [Fact]
    public async Task ItemTypesAreListedByName()
    {
        await data.TypeAsync("Weapon");
        await data.TypeAsync("Armor");

        var types = await data.Types.GetItemTypesAsync(includeArchived: true, Token);

        Assert.Equal(["Armor", "Weapon"], types.Select(type => type.Name));
    }

    [Fact]
    public async Task UnarchivedItemTypeIsListedAgain()
    {
        var weapon = await data.TypeAsync("Weapon");
        await data.Types.ArchiveItemTypeAsync(weapon.ItemTypeId, Token);

        var result = await data.Types.UnarchiveItemTypeAsync(weapon.ItemTypeId, Token);

        Assert.False(result.Value!.IsArchived);
    }

    [Fact]
    public async Task ArchivingAnUnknownItemTypeIsNotFound()
    {
        var result = await data.Types.ArchiveItemTypeAsync(Guid.NewGuid(), Token);

        Assert.Equal(ItemOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task RaritiesAreListedInCreationOrder()
    {
        await data.RarityAsync("Rare");
        await data.RarityAsync("Common");

        var rarities = await data.Rarities.GetRaritiesAsync(includeArchived: false, Token);

        Assert.Equal([("Rare", 0), ("Common", 1)], rarities.Select(rarity => (rarity.Name, rarity.Position)));
    }

    [Fact]
    public async Task MovingARarityUpChangesTheOrder()
    {
        await data.RarityAsync("Rare");
        var common = await data.RarityAsync("Common");

        await data.Rarities.MoveRarityAsync(common.RarityId, up: true, Token);

        Assert.Equal(["Common", "Rare"], (await data.Rarities.GetRaritiesAsync(false, Token)).Select(rarity => rarity.Name));
    }

    [Fact]
    public async Task MovingTheLastRarityDownKeepsTheOrder()
    {
        await data.RarityAsync("Common");
        var rare = await data.RarityAsync("Rare");

        var result = await data.Rarities.MoveRarityAsync(rare.RarityId, up: false, Token);

        Assert.Equal(new { result.Outcome, Position = 1 }, new { result.Outcome, result.Value!.Position });
    }

    [Fact]
    public async Task MovingAnUnknownRarityIsNotFound()
    {
        var result = await data.Rarities.MoveRarityAsync(Guid.NewGuid(), up: true, Token);

        Assert.Equal(ItemOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task ArchivedRarityKeepsItsPlace()
    {
        await data.RarityAsync("Common");
        var uncommon = await data.RarityAsync("Uncommon");
        await data.RarityAsync("Rare");
        await data.Rarities.ArchiveRarityAsync(uncommon.RarityId, Token);

        var rarities = await data.Rarities.GetRaritiesAsync(includeArchived: true, Token);

        Assert.Equal([("Common", false), ("Uncommon", true), ("Rare", false)], rarities.Select(rarity => (rarity.Name, rarity.IsArchived)));
    }

    [Fact]
    public async Task DuplicateRarityNameIsRejected()
    {
        await data.RarityAsync("Common");

        var result = await data.Rarities.CreateRarityAsync("common", Token);

        Assert.Equal(ItemOutcome.NameAlreadyUsed, result.Outcome);
    }

    [Fact]
    public async Task RenamedRarityKeepsItsPlace()
    {
        await data.RarityAsync("Common");
        var rare = await data.RarityAsync("Rare");

        var result = await data.Rarities.RenameRarityAsync(rare.RarityId, rare.Version, "Very rare", Token);

        Assert.Equal(new { Name = "Very rare", Position = 1 }, new { result.Value!.Name, result.Value.Position });
    }

    [Fact]
    public async Task StaleRarityRenameIsRejected()
    {
        var rare = await data.RarityAsync("Rare");
        await data.Rarities.RenameRarityAsync(rare.RarityId, rare.Version, "Very rare", Token);

        var result = await data.Rarities.RenameRarityAsync(rare.RarityId, rare.Version, "Unique", Token);

        Assert.Equal(ItemOutcome.StaleVersion, result.Outcome);
    }

    [Fact]
    public async Task ConcurrentFirstRaritiesAreNeverBothLostFromTheOrder()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await factory.ResetAsync();
            factory.Clock.FirstReadBarrier = new Barrier(2);
            var results = await Task.WhenAll(
                data.Rarities.CreateRarityAsync("Common", Token),
                data.Rarities.CreateRarityAsync("Rare", Token));
            var created = results.Count(result => result.Outcome == ItemOutcome.Succeeded);

            Assert.Equal(created, (await data.Rarities.GetRaritiesAsync(true, Token)).Select(rarity => rarity.Position).Distinct().Count());
        }
    }
}
