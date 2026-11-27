using Marten;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Domain.Events.ItemCopyEvents;
using SciFiLarpNetwork.Backend.Domain.Events.ItemEvents;
using SciFiLarpNetwork.Backend.Domain.Events.ItemTypeEvents;
using SciFiLarpNetwork.Backend.Domain.Events.RarityEvents;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Items;

/// <summary>Which events each item command records on its stream, and that refused commands record none.</summary>
[Collection("Backend database integration")]
public sealed class ItemEventTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private readonly ItemTestData data = new(factory);

    private static CancellationToken Token => ItemTestData.Token;

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task ItemChangesAreRecordedAsEvents()
    {
        var cell = await data.ItemAsync("Power cell");
        var item = await data.ItemAsync("Blaster");
        item = (await data.Items.UpdateItemAsync(item.ItemId, item.Version,
            ItemTestData.Definition("Rifle", item.ItemTypeId, item.RarityId), Token)).Value!;
        item = (await data.Items.AddComponentAsync(item.ItemId, item.Version, cell.ItemId, 1, Token)).Value!;
        item = (await data.Images.SetImageAsync(item.ItemId, item.Version, ItemTestImages.Png(), Token)).Value!;
        item = (await data.Images.RemoveImageAsync(item.ItemId, item.Version, Token)).Value!;
        await data.Items.ArchiveItemAsync(item.ItemId, Token);
        await data.Items.UnarchiveItemAsync(item.ItemId, Token);

        Assert.Equal(
            [
                typeof(ItemCreated), typeof(ItemDetailsChanged), typeof(ItemComponentsChanged), typeof(ItemImageSet),
                typeof(ItemImageRemoved), typeof(ItemArchived), typeof(ItemUnarchived)
            ],
            await EventTypesAsync(item.ItemId));
    }

    [Fact]
    public async Task StaleEditRecordsNoEvent()
    {
        var item = await data.ItemAsync("Blaster");
        await data.Items.UpdateItemAsync(item.ItemId, item.Version + 1,
            ItemTestData.Definition("Rifle", item.ItemTypeId, item.RarityId), Token);

        Assert.Equal([typeof(ItemCreated)], await EventTypesAsync(item.ItemId));
    }

    [Fact]
    public async Task RefusedSelfComponentRecordsNoEvent()
    {
        var item = await data.ItemAsync("Blaster");
        await data.Items.AddComponentAsync(item.ItemId, item.Version, item.ItemId, 1, Token);

        Assert.Equal([typeof(ItemCreated)], await EventTypesAsync(item.ItemId));
    }

    [Fact]
    public async Task RefusedZeroQuantityRecordsNoEvent()
    {
        var cell = await data.ItemAsync("Power cell");
        var item = await data.ItemAsync("Blaster");
        await data.Items.AddComponentAsync(item.ItemId, item.Version, cell.ItemId, 0, Token);

        Assert.Equal([typeof(ItemCreated)], await EventTypesAsync(item.ItemId));
    }

    [Fact]
    public async Task CopyChangesAreRecordedAsEvents()
    {
        var item = await data.ItemAsync("Medkit");
        await data.Prints.PrintAsync(Guid.NewGuid(), [item.ItemId], 1, Token);
        var copy = (await data.Copies.GetCopiesAsync(item.ItemId, Token))!.Single();
        await data.Copies.DeactivateCopyAsync(copy.CopyId, Token);
        await data.Copies.ReactivateCopyAsync(copy.CopyId, Token);

        Assert.Equal([typeof(ItemCopyPrinted), typeof(ItemCopyDeactivated), typeof(ItemCopyReactivated)],
            await EventTypesAsync(copy.CopyId));
    }

    [Fact]
    public async Task PrintIsRecordedOnItsOwnStream()
    {
        var item = await data.ItemAsync("Medkit");
        var printId = Guid.NewGuid();
        await data.Prints.PrintAsync(printId, [item.ItemId], 2, Token);

        Assert.Equal([typeof(ItemPrintCreated)], await EventTypesAsync(printId));
    }

    [Fact]
    public async Task RepeatedPrintRecordsNoMoreEvents()
    {
        var item = await data.ItemAsync("Medkit");
        var printId = Guid.NewGuid();
        await data.Prints.PrintAsync(printId, [item.ItemId], 2, Token);
        await data.Prints.PrintAsync(printId, [item.ItemId], 2, Token);

        Assert.Equal([typeof(ItemPrintCreated)], await EventTypesAsync(printId));
    }

    [Fact]
    public async Task ItemTypeChangesAreRecordedAsEvents()
    {
        var type = await data.TypeAsync("Weapon");
        await data.Types.RenameItemTypeAsync(type.ItemTypeId, type.Version, "Arms", Token);
        await data.Types.ArchiveItemTypeAsync(type.ItemTypeId, Token);
        await data.Types.UnarchiveItemTypeAsync(type.ItemTypeId, Token);

        Assert.Equal([typeof(ItemTypeCreated), typeof(ItemTypeRenamed), typeof(ItemTypeArchived), typeof(ItemTypeUnarchived)],
            await EventTypesAsync(type.ItemTypeId));
    }

    [Fact]
    public async Task RarityChangesAreRecordedAsEvents()
    {
        var rarity = await data.RarityAsync("Rare");
        await data.Rarities.RenameRarityAsync(rarity.RarityId, rarity.Version, "Very rare", Token);
        await data.Rarities.ArchiveRarityAsync(rarity.RarityId, Token);
        await data.Rarities.UnarchiveRarityAsync(rarity.RarityId, Token);

        Assert.Equal([typeof(RarityCreated), typeof(RarityRenamed), typeof(RarityArchived), typeof(RarityUnarchived)],
            await EventTypesAsync(rarity.RarityId));
    }

    [Fact]
    public async Task UnchangedEditRecordsNoEvent()
    {
        var item = await data.ItemAsync("Medkit");
        await data.Items.UpdateItemAsync(item.ItemId, item.Version,
            ItemTestData.Definition("Medkit", item.ItemTypeId, item.RarityId), Token);

        Assert.Equal([typeof(ItemCreated)], await EventTypesAsync(item.ItemId));
    }

    private async Task<Type[]> EventTypesAsync(Guid streamId)
    {
        await using var query = factory.Services.GetRequiredService<IDocumentStore>().QuerySession();
        return [.. (await query.Events.FetchStreamAsync(streamId, token: Token)).Select(stored => stored.EventType)];
    }
}
