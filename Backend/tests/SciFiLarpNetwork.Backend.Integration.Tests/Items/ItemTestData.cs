using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Domain.Items;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Items;

/// <summary>Creates item types, rarities and items through the public services.</summary>
internal sealed class ItemTestData(AdministratorAuthenticationFactory factory)
{
    public IItemTypeService Types => factory.Services.GetRequiredService<IItemTypeService>();

    public IRarityService Rarities => factory.Services.GetRequiredService<IRarityService>();

    public IItemService Items => factory.Services.GetRequiredService<IItemService>();

    public IItemImageService Images => factory.Services.GetRequiredService<IItemImageService>();

    public IItemCopyService Copies => factory.Services.GetRequiredService<IItemCopyService>();

    public IItemPrintService Prints => factory.Services.GetRequiredService<IItemPrintService>();

    public static CancellationToken Token => TestContext.Current.CancellationToken;

    public async Task<ItemTypeSummary> TypeAsync(string name = "Tool") =>
        (await Types.CreateItemTypeAsync(name, Token)).Value!;

    public async Task<RaritySummary> RarityAsync(string name = "Common") =>
        (await Rarities.CreateRarityAsync(name, Token)).Value!;

    /// <summary>The details of a valid item of a new type and rarity.</summary>
    public async Task<ItemDefinition> DefinitionAsync(string name = "Medkit") =>
        Definition(name, (await TypeAsync($"Type of {name}")).ItemTypeId, (await RarityAsync($"Rarity of {name}")).RarityId);

    public static ItemDefinition Definition(string name, Guid itemTypeId, Guid rarityId) =>
        new(name, itemTypeId, rarityId, "A small kit.", "Heals two wounds.", 25, 3, new DateOnly(2027, 5, 1));

    public async Task<ItemResult<ItemSummary>> CreateAsync(ItemDefinition definition, params ItemComponent[] components) =>
        await Items.CreateItemAsync(new ItemCreation(definition, components, null), Token);

    public async Task<ItemSummary> ItemAsync(string name = "Medkit", params ItemComponent[] components) =>
        (await CreateAsync(await DefinitionAsync(name), components)).Value!;

    public async Task<ItemSummary> ArchivedItemAsync(string name) =>
        (await Items.ArchiveItemAsync((await ItemAsync(name)).ItemId, Token)).Value!;
}
