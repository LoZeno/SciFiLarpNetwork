using Avalonia.Controls;
using Avalonia.Threading;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Tests.ViewModels.Items;
using SciFiLarpNetwork.Management.ViewModels.Items;
using SciFiLarpNetwork.Management.Views.Items;

namespace SciFiLarpNetwork.Management.Tests.Views;

public sealed class ItemWindowTests
{
    [Fact]
    public Task NewItemOpensTheItemDialogOverTheOwner() => AvaloniaUi.Run(async () =>
    {
        var owner = ShowOwner();
        var screen = await LoadedScreenAsync(new FakeItemApi(), owner);

        var dialog = await OpenAsync(owner, screen.NewCommand.ExecuteAsync());

        Assert.Equal((typeof(ItemEditorWindow), "New item"), dialog);
    });

    [Fact]
    public Task SavingTheItemDialogClosesIt() => AvaloniaUi.Run(async () =>
    {
        var owner = ShowOwner();
        var api = new FakeItemApi();
        api.AddType("Weapon");
        api.AddRarity("Rare");
        var screen = await LoadedScreenAsync(api, owner);
        var running = screen.NewCommand.ExecuteAsync();
        Dispatcher.UIThread.RunJobs();
        var editor = (ItemEditorViewModel)owner.OwnedWindows.Single().DataContext!;
        editor.Name = "Blaster";

        await editor.SaveCommand.ExecuteAsync();
        Dispatcher.UIThread.RunJobs();
        await running;

        Assert.Empty(owner.OwnedWindows);
    });

    [Fact]
    public Task AddComponentOpensTheComponentDialog() => AvaloniaUi.Run(async () =>
    {
        var owner = ShowOwner();
        var api = new FakeItemApi();
        api.AddItem("Blaster");
        var screen = await LoadedScreenAsync(api, owner);
        screen.SelectedItem = screen.Items[0];
        Dispatcher.UIThread.RunJobs();

        var dialog = await OpenAsync(owner, screen.AddComponentCommand.ExecuteAsync());

        Assert.Equal((typeof(ItemComponentEditorWindow), "Add component"), dialog);
    });

    [Fact]
    public Task CloneComponentDialogOpensOverTheItemDialog() => AvaloniaUi.Run(async () =>
    {
        var owner = ShowOwner();
        var api = new FakeItemApi();
        api.AddItem("Blaster");
        var screen = await LoadedScreenAsync(api, owner);
        screen.SelectedItem = screen.Items[0];
        Dispatcher.UIThread.RunJobs();
        var cloneRunning = screen.CloneCommand.ExecuteAsync();
        Dispatcher.UIThread.RunJobs();
        var itemWindow = owner.OwnedWindows.Single();
        var editor = (ItemEditorViewModel)itemWindow.DataContext!;

        var running = editor.AddComponentCommand.ExecuteAsync();
        Dispatcher.UIThread.RunJobs();
        var componentWindow = itemWindow.OwnedWindows.Single();
        var result = (componentWindow.GetType(), componentWindow.Title);
        componentWindow.Close();
        await running;
        itemWindow.Close();
        Dispatcher.UIThread.RunJobs();
        await cloneRunning;

        Assert.Equal((typeof(ItemComponentEditorWindow), "Add component"), result);
    });

    [Fact]
    public Task ManagingRaritiesOpensTheCatalogWindow() => AvaloniaUi.Run(async () =>
    {
        var owner = ShowOwner();
        var screen = await LoadedScreenAsync(new FakeItemApi(), owner);

        var dialog = await OpenAsync(owner, screen.ManageRaritiesCommand.ExecuteAsync());

        Assert.Equal((typeof(ItemCatalogWindow), "Rarities"), dialog);
    });

    [Fact]
    public Task NewRarityOpensTheNameDialogOverTheCatalog() => AvaloniaUi.Run(async () =>
    {
        var owner = ShowOwner();
        var screen = await LoadedScreenAsync(new FakeItemApi(), owner);
        var catalogRunning = screen.ManageRaritiesCommand.ExecuteAsync();
        Dispatcher.UIThread.RunJobs();
        var catalogWindow = owner.OwnedWindows.Single();
        var catalog = (ItemCatalogViewModel)catalogWindow.DataContext!;

        var running = catalog.NewCommand.ExecuteAsync();
        Dispatcher.UIThread.RunJobs();
        var nameWindow = catalogWindow.OwnedWindows.Single();
        var result = (nameWindow.GetType(), nameWindow.Title);
        nameWindow.Close();
        await running;
        catalog.CloseCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        await catalogRunning;

        Assert.Equal((typeof(ItemCatalogEntryWindow), "New rarity"), result);
    });

    private static async Task<(Type, string?)> OpenAsync(Window owner, Task running)
    {
        Dispatcher.UIThread.RunJobs();
        var dialog = owner.OwnedWindows.Single();
        var result = (dialog.GetType(), dialog.Title);
        dialog.Close();
        Dispatcher.UIThread.RunJobs();
        await running;
        return result;
    }

    private static async Task<ItemScreenViewModel> LoadedScreenAsync(FakeItemApi api, Window owner)
    {
        var screen = new ItemScreenViewModel(api, new JsonLocalizer(), new WindowItemDialogService(() => owner),
            "http://localhost:5000/api", "en", FakeItemApi.ItemsHref);
        await screen.LoadAsync();
        return screen;
    }

    private static Window ShowOwner()
    {
        var owner = new Window();
        owner.Show();
        return owner;
    }
}
