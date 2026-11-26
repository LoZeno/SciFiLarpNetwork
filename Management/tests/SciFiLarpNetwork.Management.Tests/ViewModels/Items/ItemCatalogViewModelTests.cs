using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.ViewModels.Items;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Items;

public sealed class ItemCatalogViewModelTests
{
    [Fact]
    public async Task NewItemTypeIsListed()
    {
        var api = new FakeItemApi();
        var dialogs = new FakeItemDialogs
        {
            OnCatalogEntryEditor = async editor =>
            {
                editor.Name = "Weapon";
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var catalog = await LoadedCatalogAsync(api, dialogs, ItemCatalogKind.ItemTypes);

        await catalog.NewCommand.ExecuteAsync();

        Assert.Equal(["Weapon"], catalog.Entries.Select(entry => entry.Name));
    }

    [Fact]
    public async Task RenamedRarityKeepsItsPlace()
    {
        var api = new FakeItemApi();
        api.AddRarity("Common");
        api.AddRarity("Rare");
        var dialogs = new FakeItemDialogs
        {
            OnCatalogEntryEditor = async editor =>
            {
                editor.Name = "Very rare";
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var catalog = await LoadedCatalogAsync(api, dialogs, ItemCatalogKind.Rarities);
        catalog.SelectedEntry = catalog.Entries[1];

        await catalog.RenameCommand.ExecuteAsync();

        Assert.Equal(["Common", "Very rare"], catalog.Entries.Select(entry => entry.Name));
    }

    [Fact]
    public async Task MovingARarityUpChangesTheOrder()
    {
        var api = new FakeItemApi();
        api.AddRarity("Common");
        api.AddRarity("Rare");
        var catalog = await LoadedCatalogAsync(api, new FakeItemDialogs(), ItemCatalogKind.Rarities);
        catalog.SelectedEntry = catalog.Entries[1];

        await catalog.MoveUpCommand.ExecuteAsync();

        Assert.Equal(["Rare", "Common"], catalog.Entries.Select(entry => entry.Name));
    }

    [Fact]
    public async Task ItemTypesCannotBeMoved()
    {
        var api = new FakeItemApi();
        api.AddType("Weapon");
        var catalog = await LoadedCatalogAsync(api, new FakeItemDialogs(), ItemCatalogKind.ItemTypes);
        catalog.SelectedEntry = catalog.Entries[0];

        Assert.False(catalog.MoveUpCommand.CanExecute(null));
    }

    [Fact]
    public async Task CancelledArchiveSendsNothing()
    {
        var api = new FakeItemApi();
        api.AddType("Weapon");
        var catalog = await LoadedCatalogAsync(api, new FakeItemDialogs { ConfirmResult = false }, ItemCatalogKind.ItemTypes);
        catalog.SelectedEntry = catalog.Entries[0];

        await catalog.ToggleArchivedCommand.ExecuteAsync();

        Assert.Empty(api.Submissions);
    }

    [Fact]
    public async Task ArchivedEntryIsHiddenAndTheChangeIsReported()
    {
        var api = new FakeItemApi();
        api.AddType("Weapon");
        var catalog = await LoadedCatalogAsync(api, new FakeItemDialogs(), ItemCatalogKind.ItemTypes);
        catalog.SelectedEntry = catalog.Entries[0];

        await catalog.ToggleArchivedCommand.ExecuteAsync();

        Assert.Equal((0, true), (catalog.Entries.Count, catalog.IsChanged));
    }

    [Fact]
    public async Task ShowingArchivedEntriesTagsThem()
    {
        var api = new FakeItemApi();
        api.AddType("Old", isArchived: true);
        var catalog = await LoadedCatalogAsync(api, new FakeItemDialogs(), ItemCatalogKind.ItemTypes);

        catalog.ShowArchived = true;

        Assert.Equal(["Old (Archived)"], catalog.Entries.Select(entry => entry.DisplayName));
    }

    private static async Task<ItemCatalogViewModel> LoadedCatalogAsync(FakeItemApi api, FakeItemDialogs dialogs, ItemCatalogKind kind)
    {
        var screen = new ItemScreenViewModel(api, new JsonLocalizer(), dialogs, "http://localhost:5000/api", "en", FakeItemApi.ItemsHref);
        await screen.LoadAsync();
        await (kind == ItemCatalogKind.Rarities ? screen.ManageRaritiesCommand : screen.ManageItemTypesCommand).ExecuteAsync();
        return dialogs.LastCatalog!;
    }
}
