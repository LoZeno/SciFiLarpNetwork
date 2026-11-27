using JasperFx;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Domain.Events.ItemEvents;
using SciFiLarpNetwork.Backend.Domain.Items;
using SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Items;

[Collection("Backend database integration")]
public sealed class ItemServiceTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private readonly ItemTestData data = new(factory);

    private static CancellationToken Token => ItemTestData.Token;

    private IItemService Items => data.Items;

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CreatedItemHasItsDetails()
    {
        var definition = await data.DefinitionAsync("  Medkit  ");

        var item = (await data.CreateAsync(definition)).Value!;

        var expected = new ItemSummary(item.ItemId, "Medkit", definition.ItemTypeId, definition.RarityId, "A small kit.",
            "Heals two wounds.", 25, 3, new DateOnly(2027, 5, 1), null, [], false, 1);

        Assert.Equal(expected, item with { Components = item.Components.Count == 0 ? expected.Components : item.Components });
    }

    [Fact]
    public async Task DuplicateItemNameIsRejectedIgnoringCase()
    {
        var first = await data.ItemAsync("Medkit");
        var definition = ItemTestData.Definition("MEDKIT", first.ItemTypeId, first.RarityId);

        var result = await data.CreateAsync(definition);

        Assert.Equal(ItemOutcome.NameAlreadyUsed, result.Outcome);
    }

    [Fact]
    public async Task ArchivedItemNameIsStillTaken()
    {
        var archived = await data.ArchivedItemAsync("Medkit");

        var result = await data.CreateAsync(ItemTestData.Definition("Medkit", archived.ItemTypeId, archived.RarityId));

        Assert.Equal(ItemOutcome.NameAlreadyUsed, result.Outcome);
    }

    [Fact]
    public async Task UnknownItemTypeIsRejected()
    {
        var rarity = await data.RarityAsync();

        var result = await data.CreateAsync(ItemTestData.Definition("Medkit", Guid.NewGuid(), rarity.RarityId));

        Assert.Equal(ItemOutcome.UnknownItemType, result.Outcome);
    }

    [Fact]
    public async Task ArchivedItemTypeIsRejectedForANewItem()
    {
        var type = await data.TypeAsync();
        var rarity = await data.RarityAsync();
        await data.Types.ArchiveItemTypeAsync(type.ItemTypeId, Token);

        var result = await data.CreateAsync(ItemTestData.Definition("Medkit", type.ItemTypeId, rarity.RarityId));

        Assert.Equal(ItemOutcome.ItemTypeArchived, result.Outcome);
    }

    [Fact]
    public async Task UnknownRarityIsRejected()
    {
        var type = await data.TypeAsync();

        var result = await data.CreateAsync(ItemTestData.Definition("Medkit", type.ItemTypeId, Guid.NewGuid()));

        Assert.Equal(ItemOutcome.UnknownRarity, result.Outcome);
    }

    [Fact]
    public async Task ArchivedRarityIsRejectedForANewItem()
    {
        var type = await data.TypeAsync();
        var rarity = await data.RarityAsync();
        await data.Rarities.ArchiveRarityAsync(rarity.RarityId, Token);

        var result = await data.CreateAsync(ItemTestData.Definition("Medkit", type.ItemTypeId, rarity.RarityId));

        Assert.Equal(ItemOutcome.RarityArchived, result.Outcome);
    }

    [Fact]
    public async Task InvalidDetailsAreRejectedAndNothingIsSaved()
    {
        var definition = await data.DefinitionAsync() with { Cost = -1 };

        await data.CreateAsync(definition);

        Assert.Equal(0, (await Items.GetItemsAsync(new ItemQuery(null, null, true, 1), Token)).TotalCount);
    }

    [Fact]
    public async Task EditMayKeepATypeArchivedSince()
    {
        var item = await data.ItemAsync();
        await data.Types.ArchiveItemTypeAsync(item.ItemTypeId, Token);

        var result = await Items.UpdateItemAsync(item.ItemId, item.Version,
            ItemTestData.Definition("Field medkit", item.ItemTypeId, item.RarityId), Token);

        Assert.Equal(ItemOutcome.Succeeded, result.Outcome);
    }

    [Fact]
    public async Task EditCannotMoveToAnArchivedRarity()
    {
        var item = await data.ItemAsync();
        var rarity = await data.RarityAsync("Rare");
        await data.Rarities.ArchiveRarityAsync(rarity.RarityId, Token);

        var result = await Items.UpdateItemAsync(item.ItemId, item.Version,
            ItemTestData.Definition("Medkit", item.ItemTypeId, rarity.RarityId), Token);

        Assert.Equal(ItemOutcome.RarityArchived, result.Outcome);
    }

    [Fact]
    public async Task StaleEditIsRejected()
    {
        var item = await data.ItemAsync();
        await Items.UpdateItemAsync(item.ItemId, item.Version, ItemTestData.Definition("Kit", item.ItemTypeId, item.RarityId), Token);

        var result = await Items.UpdateItemAsync(item.ItemId, item.Version,
            ItemTestData.Definition("Big kit", item.ItemTypeId, item.RarityId), Token);

        Assert.Equal(ItemOutcome.StaleVersion, result.Outcome);
    }

    [Fact]
    public async Task EditKeepsTheComponents()
    {
        var cell = await data.ItemAsync("Power cell");
        var item = await data.ItemAsync("Blaster", new ItemComponent(cell.ItemId, 2));

        var result = await Items.UpdateItemAsync(item.ItemId, item.Version,
            ItemTestData.Definition("Heavy blaster", item.ItemTypeId, item.RarityId), Token);

        Assert.Equal([new ItemComponentSummary(cell.ItemId, "Power cell", 2, false)], result.Value!.Components);
    }

    [Fact]
    public async Task UnchangedEditKeepsTheVersion()
    {
        var item = await data.ItemAsync();

        var result = await Items.UpdateItemAsync(item.ItemId, item.Version,
            ItemTestData.Definition(item.Name, item.ItemTypeId, item.RarityId), Token);

        Assert.Equal(1L, result.Value!.Version);
    }

    [Fact]
    public async Task ItemIsCreatedWithItsComponents()
    {
        var cell = await data.ItemAsync("Power cell");
        var casing = await data.ItemAsync("Casing");

        var item = await data.ItemAsync("Blaster", new ItemComponent(cell.ItemId, 2), new ItemComponent(casing.ItemId, 1));

        Assert.Equal(
            [new ItemComponentSummary(casing.ItemId, "Casing", 1, false), new ItemComponentSummary(cell.ItemId, "Power cell", 2, false)],
            item.Components);
    }

    [Fact]
    public async Task UnknownComponentIsRejected()
    {
        var result = await data.CreateAsync(await data.DefinitionAsync("Blaster"), new ItemComponent(Guid.NewGuid(), 1));

        Assert.Equal(ItemOutcome.UnknownComponent, result.Outcome);
    }

    [Fact]
    public async Task ArchivedComponentIsRejected()
    {
        var cell = await data.ArchivedItemAsync("Power cell");

        var result = await data.CreateAsync(await data.DefinitionAsync("Blaster"), new ItemComponent(cell.ItemId, 1));

        Assert.Equal(ItemOutcome.ComponentArchived, result.Outcome);
    }

    [Fact]
    public async Task SameRecipeAsAnotherItemIsRejected()
    {
        var cell = await data.ItemAsync("Power cell");
        await data.ItemAsync("Blaster", new ItemComponent(cell.ItemId, 2));

        var result = await data.CreateAsync(await data.DefinitionAsync("Rifle"), new ItemComponent(cell.ItemId, 2));

        Assert.Equal(ItemOutcome.DuplicateRecipe, result.Outcome);
    }

    [Fact]
    public async Task SameComponentsWithOtherQuantitiesAreAllowed()
    {
        var cell = await data.ItemAsync("Power cell");
        await data.ItemAsync("Blaster", new ItemComponent(cell.ItemId, 2));

        var result = await data.CreateAsync(await data.DefinitionAsync("Rifle"), new ItemComponent(cell.ItemId, 3));

        Assert.Equal(ItemOutcome.Succeeded, result.Outcome);
    }

    [Fact]
    public async Task ManyItemsMayHaveNoComponents()
    {
        await data.ItemAsync("Medkit");

        var result = await data.CreateAsync(await data.DefinitionAsync("Bandage"));

        Assert.Equal(ItemOutcome.Succeeded, result.Outcome);
    }

    [Fact]
    public async Task RecipeIndexRejectsASecondItemWithTheSameRecipe()
    {
        var cell = await data.ItemAsync("Power cell");
        var blaster = await data.ItemAsync("Blaster", new ItemComponent(cell.ItemId, 2));
        var itemId = Guid.NewGuid();
        await using var session = factory.Services.GetRequiredService<IDocumentStore>().LightweightSession();
        ItemComponent[] components = [new(cell.ItemId, 2)];
        session.Events.StartStream<ItemDocument>(itemId, new ItemCreated(itemId, "Rifle", "RIFLE", blaster.ItemTypeId,
            blaster.RarityId, null, null, 0, null, null, components, ItemRecipes.Key(itemId, components), null, DateTimeOffset.UtcNow));

        var exception = await Record.ExceptionAsync(() => session.SaveChangesAsync(Token));

        Assert.IsType<DocumentAlreadyExistsException>(exception);
    }

    [Fact]
    public async Task AddedComponentIsListed()
    {
        var cell = await data.ItemAsync("Power cell");
        var blaster = await data.ItemAsync("Blaster");

        var result = await Items.AddComponentAsync(blaster.ItemId, blaster.Version, cell.ItemId, 2, Token);

        Assert.Equal([new ItemComponentSummary(cell.ItemId, "Power cell", 2, false)], result.Value!.Components);
    }

    [Fact]
    public async Task AddingAnExistingComponentIsRejected()
    {
        var cell = await data.ItemAsync("Power cell");
        var blaster = await data.ItemAsync("Blaster", new ItemComponent(cell.ItemId, 1));

        var result = await Items.AddComponentAsync(blaster.ItemId, blaster.Version, cell.ItemId, 2, Token);

        Assert.Equal(ItemOutcome.ComponentAlreadyAdded, result.Outcome);
    }

    [Fact]
    public async Task AddingTheItemToItselfIsRejected()
    {
        var blaster = await data.ItemAsync("Blaster");

        var result = await Items.AddComponentAsync(blaster.ItemId, blaster.Version, blaster.ItemId, 1, Token);

        Assert.Equal(ItemOutcome.SelfComponent, result.Outcome);
    }

    [Fact]
    public async Task AddingAComponentThatContainsTheItemIsALoop()
    {
        var cell = await data.ItemAsync("Power cell");
        var core = await data.ItemAsync("Core", new ItemComponent(cell.ItemId, 1));
        var blaster = await data.ItemAsync("Blaster", new ItemComponent(core.ItemId, 1));
        var current = (await Items.GetItemAsync(cell.ItemId, Token))!;

        var result = await Items.AddComponentAsync(cell.ItemId, current.Version, blaster.ItemId, 1, Token);

        Assert.Equal(ItemOutcome.ComponentLoop, result.Outcome);
    }

    [Fact]
    public async Task AddingAComponentThatContainsTheItemSeveralLevelsDownIsALoop()
    {
        var cell = await data.ItemAsync("Power cell");
        var core = await data.ItemAsync("Core", new ItemComponent(cell.ItemId, 1));
        var housing = await data.ItemAsync("Housing", new ItemComponent(core.ItemId, 1));
        var frame = await data.ItemAsync("Frame", new ItemComponent(housing.ItemId, 2));
        var blaster = await data.ItemAsync("Blaster", new ItemComponent(frame.ItemId, 1));
        var current = (await Items.GetItemAsync(cell.ItemId, Token))!;

        var result = await Items.AddComponentAsync(cell.ItemId, current.Version, blaster.ItemId, 1, Token);

        Assert.Equal(ItemOutcome.ComponentLoop, result.Outcome);
    }

    [Fact]
    public async Task AddingAComponentFromAnotherBranchIsNoLoop()
    {
        var cell = await data.ItemAsync("Power cell");
        var core = await data.ItemAsync("Core", new ItemComponent(cell.ItemId, 1));
        var housing = await data.ItemAsync("Housing", new ItemComponent(core.ItemId, 1));
        var scope = await data.ItemAsync("Scope");
        var current = (await Items.GetItemAsync(scope.ItemId, Token))!;

        var result = await Items.AddComponentAsync(scope.ItemId, current.Version, housing.ItemId, 1, Token);

        Assert.Equal(ItemOutcome.Succeeded, result.Outcome);
    }

    [Fact]
    public async Task ArchivedItemIsMarkedArchived()
    {
        var medkit = await data.ItemAsync("Medkit");

        var result = await Items.ArchiveItemAsync(medkit.ItemId, Token);

        Assert.True(result.Value!.IsArchived);
    }

    [Fact]
    public async Task UnarchivedItemIsListedAgain()
    {
        var medkit = await data.ArchivedItemAsync("Medkit");
        await Items.UnarchiveItemAsync(medkit.ItemId, Token);

        var page = await Items.GetItemsAsync(new ItemQuery(null, null, false, 1), Token);

        Assert.Equal(["Medkit"], page.Items.Select(item => item.Name));
    }

    [Fact]
    public async Task ArchivingAnUnknownItemIsNotFound()
    {
        var result = await Items.ArchiveItemAsync(Guid.NewGuid(), Token);

        Assert.Equal(ItemOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task AddingAComponentWithAZeroQuantityIsRejected()
    {
        var cell = await data.ItemAsync("Power cell");
        var blaster = await data.ItemAsync("Blaster");

        var result = await Items.AddComponentAsync(blaster.ItemId, blaster.Version, cell.ItemId, 0, Token);

        Assert.Equal(ItemOutcome.InvalidQuantity, result.Outcome);
    }

    [Fact]
    public async Task AddingFromAStaleVersionIsRejected()
    {
        var cell = await data.ItemAsync("Power cell");
        var blaster = await data.ItemAsync("Blaster");
        await Items.UpdateItemAsync(blaster.ItemId, blaster.Version, ItemTestData.Definition("Rifle", blaster.ItemTypeId, blaster.RarityId), Token);

        var result = await Items.AddComponentAsync(blaster.ItemId, blaster.Version, cell.ItemId, 1, Token);

        Assert.Equal(ItemOutcome.StaleVersion, result.Outcome);
    }

    [Fact]
    public async Task ChangedQuantityIsListed()
    {
        var cell = await data.ItemAsync("Power cell");
        var blaster = await data.ItemAsync("Blaster", new ItemComponent(cell.ItemId, 1));

        var result = await Items.ChangeComponentQuantityAsync(blaster.ItemId, blaster.Version, cell.ItemId, 4, Token);

        Assert.Equal(4, result.Value!.Components.Single().Quantity);
    }

    [Fact]
    public async Task ChangingTheQuantityOfANonComponentIsRejected()
    {
        var cell = await data.ItemAsync("Power cell");
        var blaster = await data.ItemAsync("Blaster");

        var result = await Items.ChangeComponentQuantityAsync(blaster.ItemId, blaster.Version, cell.ItemId, 4, Token);

        Assert.Equal(ItemOutcome.NotAComponent, result.Outcome);
    }

    [Fact]
    public async Task QuantityAboveTheMaximumIsRejected()
    {
        var cell = await data.ItemAsync("Power cell");
        var blaster = await data.ItemAsync("Blaster", new ItemComponent(cell.ItemId, 1));

        var result = await Items.ChangeComponentQuantityAsync(blaster.ItemId, blaster.Version, cell.ItemId, 100, Token);

        Assert.Equal(ItemOutcome.InvalidQuantity, result.Outcome);
    }

    [Fact]
    public async Task RemovedComponentIsNoLongerListed()
    {
        var cell = await data.ItemAsync("Power cell");
        var blaster = await data.ItemAsync("Blaster", new ItemComponent(cell.ItemId, 1));

        var result = await Items.RemoveComponentAsync(blaster.ItemId, blaster.Version, cell.ItemId, Token);

        Assert.Empty(result.Value!.Components);
    }

    [Fact]
    public async Task RemovalThatLeavesAnotherItemsRecipeIsRejected()
    {
        var cell = await data.ItemAsync("Power cell");
        var casing = await data.ItemAsync("Casing");
        await data.ItemAsync("Pistol", new ItemComponent(cell.ItemId, 1));
        var blaster = await data.ItemAsync("Blaster", new ItemComponent(cell.ItemId, 1), new ItemComponent(casing.ItemId, 1));

        var result = await Items.RemoveComponentAsync(blaster.ItemId, blaster.Version, casing.ItemId, Token);

        Assert.Equal(ItemOutcome.DuplicateRecipe, result.Outcome);
    }

    [Fact]
    public async Task ArchivedComponentIsKeptAndMarked()
    {
        var cell = await data.ItemAsync("Power cell");
        var blaster = await data.ItemAsync("Blaster", new ItemComponent(cell.ItemId, 1));
        await Items.ArchiveItemAsync(cell.ItemId, Token);

        var current = await Items.GetItemAsync(blaster.ItemId, Token);

        Assert.Equal([new ItemComponentSummary(cell.ItemId, "Power cell", 1, true)], current!.Components);
    }

    [Fact]
    public async Task CloneGetsTheSourcePicture()
    {
        var source = await data.ItemAsync("Medkit");
        var withImage = (await data.Images.SetImageAsync(source.ItemId, source.Version, ItemTestImages.Png(), Token)).Value!;

        var clone = await Items.CreateItemAsync(
            new ItemCreation(ItemTestData.Definition("Field medkit", source.ItemTypeId, source.RarityId), [], source.ItemId), Token);

        Assert.Equal(withImage.ImageId, clone.Value!.ImageId);
    }

    [Fact]
    public async Task CloneWithTheSameComponentsIsRejectedAsADuplicateRecipe()
    {
        var cell = await data.ItemAsync("Power cell");
        var source = await data.ItemAsync("Blaster", new ItemComponent(cell.ItemId, 2));

        var clone = await Items.CreateItemAsync(
            new ItemCreation(ItemTestData.Definition("Rifle", source.ItemTypeId, source.RarityId), [new(cell.ItemId, 2)], source.ItemId), Token);

        Assert.Equal(ItemOutcome.DuplicateRecipe, clone.Outcome);
    }

    [Fact]
    public async Task CloneOfAnUnknownItemIsRejected()
    {
        var definition = await data.DefinitionAsync();

        var clone = await Items.CreateItemAsync(new ItemCreation(definition, [], Guid.NewGuid()), Token);

        Assert.Equal(ItemOutcome.UnknownCloneSource, clone.Outcome);
    }

    [Fact]
    public async Task SearchMatchesTheEffectIgnoringCase()
    {
        await data.ItemAsync("Medkit");
        var definition = await data.DefinitionAsync("Grenade") with { Effect = "Deals three wounds." };
        await data.CreateAsync(definition);

        var page = await Items.GetItemsAsync(new ItemQuery("THREE WOUNDS", null, false, 1), Token);

        Assert.Equal(["Grenade"], page.Items.Select(item => item.Name));
    }

    [Fact]
    public async Task FilterByTypeListsOnlyThatType()
    {
        var medkit = await data.ItemAsync("Medkit");
        await data.ItemAsync("Grenade");

        var page = await Items.GetItemsAsync(new ItemQuery(null, medkit.ItemTypeId, false, 1), Token);

        Assert.Equal(["Medkit"], page.Items.Select(item => item.Name));
    }

    [Fact]
    public async Task ArchivedItemsAreHiddenByDefault()
    {
        await data.ArchivedItemAsync("Medkit");
        await data.ItemAsync("Grenade");

        var page = await Items.GetItemsAsync(new ItemQuery(null, null, false, 1), Token);

        Assert.Equal(["Grenade"], page.Items.Select(item => item.Name));
    }

    [Fact]
    public async Task ItemsAreListedByNameInPagesOfFifty()
    {
        var type = await data.TypeAsync();
        var rarity = await data.RarityAsync();
        foreach (var number in Enumerable.Range(1, 51))
        {
            await data.CreateAsync(ItemTestData.Definition($"Item {number:D2}", type.ItemTypeId, rarity.RarityId));
        }

        var page = await Items.GetItemsAsync(new ItemQuery(null, null, false, 2), Token);

        Assert.Equal(("Item 51", 2, 51L), (string.Join(',', page.Items.Select(item => item.Name)), page.PageCount, page.TotalCount));
    }

    [Fact]
    public async Task ConcurrentOppositeAdditionsNeverBothSucceed()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await factory.ResetAsync();
            var first = await data.ItemAsync("First");
            var second = await data.ItemAsync("Second");
            factory.Clock.FirstReadBarrier = new Barrier(2);

            var results = await Task.WhenAll(
                Items.AddComponentAsync(first.ItemId, first.Version, second.ItemId, 1, Token),
                Items.AddComponentAsync(second.ItemId, second.Version, first.ItemId, 1, Token));

            Assert.True(results.Count(result => result.Outcome == ItemOutcome.Succeeded) <= 1);
        }
    }
}
