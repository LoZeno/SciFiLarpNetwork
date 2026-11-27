using System.Text.Json;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.ViewModels.Items;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Items;

public sealed class ItemScreenViewModelTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01];

    [Fact]
    public async Task LoadListsItemsWithTheirType()
    {
        var api = new FakeItemApi();
        var weapon = api.AddType("Weapon");
        api.AddItem("Blaster", weapon);
        var screen = CreateScreen(api);

        await screen.LoadAsync();

        Assert.Equal(["Blaster — Weapon"], screen.Items.Select(item => item.DisplayName));
    }

    [Fact]
    public async Task ShowingArchivedItemsTagsThem()
    {
        var api = new FakeItemApi();
        api.AddItem("Old kit", isArchived: true);
        var screen = await LoadedScreenAsync(api);

        screen.ShowArchived = true;

        Assert.Equal(["Old kit (Archived) — Tool"], screen.Items.Select(item => item.DisplayName));
    }

    [Fact]
    public async Task TypeFilterOffersAllTypesFirst()
    {
        var api = new FakeItemApi();
        api.AddType("Weapon");
        api.AddType("Armor");
        api.AddType("Old", isArchived: true);

        var screen = await LoadedScreenAsync(api);

        Assert.Equal(["All types", "Armor", "Weapon"], screen.TypeFilters.Select(choice => choice.Name));
    }

    [Fact]
    public async Task ChoosingATypeListsOnlyThatType()
    {
        var api = new FakeItemApi();
        var weapon = api.AddType("Weapon");
        var armor = api.AddType("Armor");
        api.AddItem("Blaster", weapon);
        api.AddItem("Vest", armor);
        var screen = await LoadedScreenAsync(api);

        screen.SelectedTypeFilter = screen.TypeFilters.Single(choice => choice.Id == armor.Id);

        Assert.Equal(["Vest"], screen.Items.Select(item => item.Name));
    }

    [Fact]
    public async Task SearchListsMatchingItems()
    {
        var api = new FakeItemApi();
        api.AddItem("Blaster");
        api.AddItem("Medkit");
        var screen = await LoadedScreenAsync(api);
        screen.Search = "med";

        await screen.SearchCommand.ExecuteAsync();

        Assert.Equal(["Medkit"], screen.Items.Select(item => item.Name));
    }

    [Fact]
    public async Task NextShowsTheFollowingPage()
    {
        var api = new FakeItemApi();
        api.AddItem("Alpha");
        api.AddItem("Beta");
        api.AddItem("Gamma");
        var screen = await LoadedScreenAsync(api);

        await screen.NextCommand.ExecuteAsync();

        Assert.Equal(["Gamma"], screen.Items.Select(item => item.Name));
    }

    [Fact]
    public async Task ASlowerEarlierListNeverReplacesANewerOne()
    {
        var api = new FakeItemApi();
        var weapon = api.AddType("Weapon");
        api.AddItem("Blaster", weapon);
        api.AddItem("Medkit");
        var screen = await LoadedScreenAsync(api);
        var gate = new TaskCompletionSource();
        api.HeldGet = ("/api/v1/items?search=", gate);
        screen.Search = "blaster";
        var slow = screen.SearchCommand.ExecuteAsync();

        screen.Search = string.Empty;
        screen.ShowArchived = true;
        gate.SetResult();
        await slow;

        Assert.Equal(["Blaster", "Medkit"], screen.Items.Select(item => item.Name));
    }

    [Fact]
    public async Task SelectingAnItemShowsItsDetails()
    {
        var api = new FakeItemApi();
        var item = api.AddItem("Medkit");
        item.Cost = 25;
        item.Effect = "Heals two wounds.";
        var screen = await LoadedScreenAsync(api);

        screen.SelectedItem = screen.Items[0];

        Assert.Equal(("Medkit", "Tool", "Common", "25", "Heals two wounds."),
            (screen.DetailName, screen.DetailType, screen.DetailRarity, screen.DetailCost, screen.DetailEffect));
    }

    [Fact]
    public async Task SelectingAnItemListsItsComponents()
    {
        var api = new FakeItemApi();
        var cell = api.AddItem("Power cell");
        var blaster = api.AddItem("Blaster");
        blaster.Components.Add((cell.Id, 2));
        var screen = await LoadedScreenAsync(api);

        screen.SelectedItem = screen.Items.Single(item => item.Name == "Blaster");

        Assert.Equal(["2 × Power cell"], screen.Components.Select(component => component.DisplayName));
    }

    [Fact]
    public async Task SelectingAnItemListsItsCopiesWithTheirState()
    {
        var api = new FakeItemApi();
        var medkit = api.AddItem("Medkit");
        var copy = api.AddCopy(medkit, isActive: false);
        var screen = await LoadedScreenAsync(api);

        screen.SelectedItem = screen.Items[0];

        Assert.StartsWith($"{copy.Id:N} (Deactivated)", screen.Copies.Single().DisplayName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NewItemSendsItsDetails()
    {
        var api = new FakeItemApi();
        var type = api.AddType("Weapon");
        var rarity = api.AddRarity("Rare");
        var dialogs = new FakeItemDialogs
        {
            OnItemEditor = async editor =>
            {
                editor.Name = "Blaster";
                editor.SelectedType = editor.Types.Single(choice => choice.Id == type.Id);
                editor.SelectedRarity = editor.Rarities.Single(choice => choice.Id == rarity.Id);
                editor.CostText = "40";
                editor.ChargesText = "6";
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await LoadedScreenAsync(api, dialogs);

        await screen.NewCommand.ExecuteAsync();

        var body = JsonDocument.Parse(api.Submissions.Single(submission => submission.ActionName == "create-item").Body!).RootElement;
        Assert.Equal(("Blaster", type.Id, rarity.Id, 40, 6),
            (body.GetProperty("name").GetString(), body.GetProperty("itemTypeId").GetGuid(), body.GetProperty("rarityId").GetGuid(),
                body.GetProperty("cost").GetInt32(), body.GetProperty("charges").GetInt32()));
    }

    [Fact]
    public async Task ANonNumericCostSendsNothing()
    {
        var api = new FakeItemApi();
        api.AddType("Weapon");
        api.AddRarity("Rare");
        var dialogs = new FakeItemDialogs
        {
            OnItemEditor = async editor =>
            {
                editor.Name = "Blaster";
                editor.CostText = "a lot";
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await LoadedScreenAsync(api, dialogs);

        await screen.NewCommand.ExecuteAsync();

        Assert.Equal(("Enter a cost from 0 to 999,999.", 0), (dialogs.LastEditor!.ErrorMessage, api.Submissions.Count));
    }

    [Fact]
    public async Task ChosenPictureIsUploadedAfterTheDetails()
    {
        var api = new FakeItemApi();
        api.AddType("Weapon");
        api.AddRarity("Rare");
        var dialogs = new FakeItemDialogs
        {
            PickedImage = Png,
            OnItemEditor = async editor =>
            {
                editor.Name = "Blaster";
                await editor.ChooseImageCommand.ExecuteAsync();
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await LoadedScreenAsync(api, dialogs);

        await screen.NewCommand.ExecuteAsync();

        Assert.Equal(["create-item", "set-item-image"], api.Submissions.Select(submission => submission.ActionName));
    }

    [Fact]
    public async Task EditSendsTheVersionItWasBasedOn()
    {
        var api = new FakeItemApi();
        api.AddItem("Medkit").Version = 4;
        var dialogs = new FakeItemDialogs
        {
            OnItemEditor = async editor =>
            {
                editor.Name = "Field medkit";
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await LoadedScreenAsync(api, dialogs);
        screen.SelectedItem = screen.Items[0];

        await screen.EditCommand.ExecuteAsync();

        Assert.Equal(4, JsonDocument.Parse(api.Submissions.Single().Body!).RootElement.GetProperty("version").GetInt64());
    }

    [Fact]
    public async Task CloneSendsTheSourceAndItsComponents()
    {
        var api = new FakeItemApi();
        var cell = api.AddItem("Power cell");
        var blaster = api.AddItem("Blaster");
        blaster.Components.Add((cell.Id, 2));
        var dialogs = new FakeItemDialogs
        {
            OnItemEditor = async editor =>
            {
                editor.Name = "Rifle";
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await LoadedScreenAsync(api, dialogs);
        screen.SelectedItem = screen.Items.Single(item => item.Name == "Blaster");

        await screen.CloneCommand.ExecuteAsync();

        var body = JsonDocument.Parse(api.Submissions.Single().Body!).RootElement;
        Assert.Equal((blaster.Id, cell.Id, 2),
            (body.GetProperty("clonedFromItemId").GetGuid(), body.GetProperty("components")[0].GetProperty("itemId").GetGuid(),
                body.GetProperty("components")[0].GetProperty("quantity").GetInt32()));
    }

    [Fact]
    public async Task CloneDialogStartsWithTheSourceComponents()
    {
        var api = new FakeItemApi();
        var cell = api.AddItem("Power cell");
        api.AddItem("Blaster").Components.Add((cell.Id, 2));
        var dialogs = new FakeItemDialogs();
        var screen = await LoadedScreenAsync(api, dialogs);
        screen.SelectedItem = screen.Items.Single(item => item.Name == "Blaster");

        await screen.CloneCommand.ExecuteAsync();

        Assert.Equal(["2 × Power cell"], dialogs.LastEditor!.Components.Select(component => component.DisplayName));
    }

    [Fact]
    public async Task ComponentRemovedInTheCloneDialogIsNotSent()
    {
        var api = new FakeItemApi();
        var cell = api.AddItem("Power cell");
        var scope = api.AddItem("Scope");
        var blaster = api.AddItem("Blaster");
        blaster.Components.AddRange([(cell.Id, 2), (scope.Id, 1)]);
        var dialogs = new FakeItemDialogs
        {
            OnItemEditor = async editor =>
            {
                editor.Name = "Pistol";
                editor.SelectedComponent = editor.Components.Single(component => component.ItemId == scope.Id);
                editor.RemoveComponentCommand.Execute(null);
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await LoadedScreenAsync(api, dialogs);
        screen.SelectedItem = screen.Items.Single(item => item.Name == "Blaster");

        await screen.CloneCommand.ExecuteAsync();

        Assert.Equal([(cell.Id, 2)], SentComponents(api));
    }

    [Fact]
    public async Task ComponentAddedInTheCloneDialogIsSent()
    {
        var api = new FakeItemApi();
        var cell = api.AddItem("Power cell");
        var scope = api.AddItem("Scope");
        api.AddItem("Blaster").Components.Add((cell.Id, 2));
        var dialogs = new FakeItemDialogs
        {
            OnItemEditor = async editor =>
            {
                editor.Name = "Rifle";
                await editor.AddComponentCommand.ExecuteAsync();
                await editor.SaveCommand.ExecuteAsync();
            },
            OnComponentEditor = async editor =>
            {
                editor.SearchText = "Scope";
                await editor.SearchCommand.ExecuteAsync();
                editor.SelectedItem = editor.Choices.Single(choice => choice.Id == scope.Id);
                editor.QuantityText = "3";
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await LoadedScreenAsync(api, dialogs);
        screen.SelectedItem = screen.Items.Single(item => item.Name == "Blaster");

        await screen.CloneCommand.ExecuteAsync();

        Assert.Equal([(cell.Id, 2), (scope.Id, 3)], SentComponents(api));
    }

    [Fact]
    public async Task CloneDialogRefusesAComponentItAlreadyHas()
    {
        var api = new FakeItemApi();
        var cell = api.AddItem("Power cell");
        api.AddItem("Blaster").Components.Add((cell.Id, 2));
        var dialogs = new FakeItemDialogs
        {
            OnItemEditor = editor => editor.AddComponentCommand.ExecuteAsync(),
            OnComponentEditor = async editor =>
            {
                editor.SelectedItem = editor.Choices.Single(choice => choice.Id == cell.Id);
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await LoadedScreenAsync(api, dialogs);
        screen.SelectedItem = screen.Items.Single(item => item.Name == "Blaster");

        await screen.CloneCommand.ExecuteAsync();

        Assert.Equal("That item is already a component. Change its quantity instead.", dialogs.LastComponentEditor!.ErrorMessage);
    }

    [Fact]
    public async Task QuantityChangedInTheCloneDialogIsSent()
    {
        var api = new FakeItemApi();
        var cell = api.AddItem("Power cell");
        api.AddItem("Blaster").Components.Add((cell.Id, 2));
        var dialogs = new FakeItemDialogs
        {
            OnItemEditor = async editor =>
            {
                editor.Name = "Rifle";
                editor.SelectedComponent = editor.Components.Single();
                await editor.ChangeQuantityCommand.ExecuteAsync();
                await editor.SaveCommand.ExecuteAsync();
            },
            OnComponentEditor = async editor =>
            {
                editor.QuantityText = "5";
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await LoadedScreenAsync(api, dialogs);
        screen.SelectedItem = screen.Items.Single(item => item.Name == "Blaster");

        await screen.CloneCommand.ExecuteAsync();

        Assert.Equal([(cell.Id, 5)], SentComponents(api));
    }

    [Fact]
    public async Task CloneComponentsAreFixedOnceTheCloneIsCreated()
    {
        var api = new FakeItemApi();
        api.AddItem("Blaster");
        api.SubmitFailures["set-item-image"] = new HttpRequestException("The connection was lost.");
        var dialogs = new FakeItemDialogs
        {
            PickedImage = Png,
            OnItemEditor = async editor =>
            {
                editor.Name = "Rifle";
                await editor.ChooseImageCommand.ExecuteAsync();
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await LoadedScreenAsync(api, dialogs);
        screen.SelectedItem = screen.Items[0];

        await screen.CloneCommand.ExecuteAsync();

        Assert.False(dialogs.LastEditor!.AddComponentCommand.CanExecute(null));
    }

    [Fact]
    public async Task EditDialogDoesNotEditComponents()
    {
        var api = new FakeItemApi();
        api.AddItem("Blaster");
        var dialogs = new FakeItemDialogs();
        var screen = await LoadedScreenAsync(api, dialogs);
        screen.SelectedItem = screen.Items[0];

        await screen.EditCommand.ExecuteAsync();

        Assert.False(dialogs.LastEditor!.EditsComponents);
    }

    [Fact]
    public async Task CloneDialogShowsTheCloneNote()
    {
        var api = new FakeItemApi();
        api.AddItem("Blaster");
        var dialogs = new FakeItemDialogs();
        var screen = await LoadedScreenAsync(api, dialogs);
        screen.SelectedItem = screen.Items[0];

        await screen.CloneCommand.ExecuteAsync();

        Assert.True(dialogs.LastEditor!.IsClone);
    }

    [Fact]
    public async Task CancelledArchiveSendsNothing()
    {
        var api = new FakeItemApi();
        api.AddItem("Medkit");
        var screen = await LoadedScreenAsync(api, new FakeItemDialogs { ConfirmResult = false });
        screen.SelectedItem = screen.Items[0];

        await screen.ToggleArchivedCommand.ExecuteAsync();

        Assert.Empty(api.Submissions);
    }

    [Fact]
    public async Task ConfirmedArchiveArchivesTheItem()
    {
        var api = new FakeItemApi();
        var medkit = api.AddItem("Medkit");
        var screen = await LoadedScreenAsync(api);
        screen.SelectedItem = screen.Items[0];

        await screen.ToggleArchivedCommand.ExecuteAsync();

        Assert.True(medkit.IsArchived);
    }

    [Fact]
    public async Task AddedComponentIsListed()
    {
        var api = new FakeItemApi();
        api.AddItem("Power cell");
        api.AddItem("Blaster");
        var dialogs = new FakeItemDialogs
        {
            OnComponentEditor = async editor =>
            {
                editor.SelectedItem = editor.Choices.Single(choice => choice.Name == "Power cell");
                editor.QuantityText = "3";
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await LoadedScreenAsync(api, dialogs);
        screen.SelectedItem = screen.Items.Single(item => item.Name == "Blaster");

        await screen.AddComponentCommand.ExecuteAsync();

        Assert.Equal(["3 × Power cell"], screen.Components.Select(component => component.DisplayName));
    }

    [Fact]
    public async Task ComponentChoicesLeaveOutTheItemItself()
    {
        var api = new FakeItemApi();
        api.AddItem("Power cell");
        api.AddItem("Blaster");
        var dialogs = new FakeItemDialogs();
        var screen = await LoadedScreenAsync(api, dialogs);
        screen.SelectedItem = screen.Items.Single(item => item.Name == "Blaster");

        await screen.AddComponentCommand.ExecuteAsync();

        Assert.Equal(["Power cell"], dialogs.LastComponentEditor!.Choices.Select(choice => choice.Name));
    }

    [Fact]
    public async Task ChangedQuantityIsListed()
    {
        var api = new FakeItemApi();
        var cell = api.AddItem("Power cell");
        api.AddItem("Blaster").Components.Add((cell.Id, 1));
        var dialogs = new FakeItemDialogs
        {
            OnComponentEditor = async editor =>
            {
                editor.QuantityText = "5";
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await LoadedScreenAsync(api, dialogs);
        screen.SelectedItem = screen.Items.Single(item => item.Name == "Blaster");
        screen.SelectedComponent = screen.Components[0];

        await screen.ChangeQuantityCommand.ExecuteAsync();

        Assert.Equal(["5 × Power cell"], screen.Components.Select(component => component.DisplayName));
    }

    [Fact]
    public async Task ConfirmedRemovalEmptiesTheComponents()
    {
        var api = new FakeItemApi();
        var cell = api.AddItem("Power cell");
        api.AddItem("Blaster").Components.Add((cell.Id, 1));
        var screen = await LoadedScreenAsync(api);
        screen.SelectedItem = screen.Items.Single(item => item.Name == "Blaster");
        screen.SelectedComponent = screen.Components[0];

        await screen.RemoveComponentCommand.ExecuteAsync();

        Assert.Empty(screen.Components);
    }

    [Fact]
    public async Task PrintSendsTheSelectedItemsAndCopyCount()
    {
        var api = new FakeItemApi();
        var medkit = api.AddItem("Medkit");
        var screen = await SelectedScreenAsync(api);
        screen.CopiesPerItemText = "3";

        await screen.PrintCommand.ExecuteAsync();

        var body = JsonDocument.Parse(api.Submissions.Single().Body!).RootElement;
        Assert.Equal((medkit.Id, 3), (body.GetProperty("itemIds")[0].GetGuid(), body.GetProperty("copiesPerItem").GetInt32()));
    }

    [Fact]
    public async Task PrintedCopiesAreListed()
    {
        var api = new FakeItemApi();
        api.AddItem("Medkit");
        var screen = await SelectedScreenAsync(api);
        screen.CopiesPerItemText = "2";

        await screen.PrintCommand.ExecuteAsync();

        Assert.Equal(2, screen.Copies.Count);
    }

    [Fact]
    public async Task PrintedCardsAreOpened()
    {
        var api = new FakeItemApi();
        api.AddItem("Medkit");
        var dialogs = new FakeItemDialogs();
        var screen = await SelectedScreenAsync(api, dialogs);

        await screen.PrintCommand.ExecuteAsync();

        Assert.Single(dialogs.OpenedPdfs);
    }

    [Fact]
    public async Task CardsThatCannotBeOpenedPointToReprint()
    {
        var api = new FakeItemApi();
        api.AddItem("Medkit");
        var screen = await SelectedScreenAsync(api, new FakeItemDialogs { OpenPdfResult = false });

        await screen.PrintCommand.ExecuteAsync();

        Assert.Contains("Reprint", screen.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CancelledPrintMakesNoCopies()
    {
        var api = new FakeItemApi();
        api.AddItem("Medkit");
        var screen = await SelectedScreenAsync(api, new FakeItemDialogs { ConfirmResult = false });

        await screen.PrintCommand.ExecuteAsync();

        Assert.Empty(api.Submissions);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("51")]
    [InlineData("many")]
    public async Task CopyCountOutsideTheRangeSendsNothing(string copies)
    {
        var api = new FakeItemApi();
        api.AddItem("Medkit");
        var screen = await SelectedScreenAsync(api);
        screen.CopiesPerItemText = copies;

        await screen.PrintCommand.ExecuteAsync();

        Assert.Equal(("Enter 1 to 50 copies of each item.", 0), (screen.StatusMessage, api.Submissions.Count));
    }

    [Fact]
    public async Task PreviewSendsThePreviewAction()
    {
        var api = new FakeItemApi();
        api.AddItem("Medkit");
        var screen = await SelectedScreenAsync(api);

        await screen.PreviewCommand.ExecuteAsync();

        Assert.Equal(["preview-item-cards"], api.Submissions.Select(submission => submission.ActionName));
    }

    [Fact]
    public async Task ReprintSendsTheSelectedCopies()
    {
        var api = new FakeItemApi();
        var copy = api.AddCopy(api.AddItem("Medkit"));
        var screen = await SelectedScreenAsync(api);
        screen.SelectedCopies.Add(screen.Copies[0]);

        await screen.ReprintCommand.ExecuteAsync();

        Assert.Equal(copy.Id, JsonDocument.Parse(api.Submissions.Single().Body!).RootElement.GetProperty("copyIds")[0].GetGuid());
    }

    [Fact]
    public async Task PrintRetriedAfterALostAnswerResendsTheSamePrintId()
    {
        var api = new FakeItemApi();
        api.AddItem("Medkit");
        var screen = await SelectedScreenAsync(api);
        api.SubmitFailures["print-item-cards"] = new HttpRequestException("The connection was lost.");
        await screen.PrintCommand.ExecuteAsync();

        await screen.PrintCommand.ExecuteAsync();

        Assert.Single(api.Submissions.Select(submission => PrintId(submission.Body)).Distinct());
    }

    [Fact]
    public async Task PrintAfterAnAnsweredPrintSendsANewPrintId()
    {
        var api = new FakeItemApi();
        api.AddItem("Medkit");
        var screen = await SelectedScreenAsync(api);
        await screen.PrintCommand.ExecuteAsync();

        await screen.PrintCommand.ExecuteAsync();

        Assert.Equal(2, api.Submissions.Select(submission => PrintId(submission.Body)).Distinct().Count());
    }

    [Fact]
    public async Task RetryWithAnotherCopyCountSendsANewPrintId()
    {
        var api = new FakeItemApi();
        api.AddItem("Medkit");
        var screen = await SelectedScreenAsync(api);
        api.SubmitFailures["print-item-cards"] = new HttpRequestException("The connection was lost.");
        await screen.PrintCommand.ExecuteAsync();
        screen.CopiesPerItemText = "2";

        await screen.PrintCommand.ExecuteAsync();

        Assert.Equal(2, api.Submissions.Select(submission => PrintId(submission.Body)).Distinct().Count());
    }

    [Fact]
    public async Task CopiesAreGroupedByPrint()
    {
        var api = new FakeItemApi();
        var medkit = api.AddItem("Medkit");
        var first = Guid.NewGuid();
        api.AddCopy(medkit, printId: first);
        api.AddCopy(medkit, printId: first);
        api.AddCopy(medkit, isActive: false, printId: Guid.NewGuid());

        var screen = await SelectedScreenAsync(api);

        Assert.Equal(
            ["by operator. Copies: 1, active: 0", "by operator. Copies: 2, active: 2"],
            screen.Prints.Select(print => print.DisplayName[print.DisplayName.IndexOf("by", StringComparison.Ordinal)..]));
    }

    [Fact]
    public async Task CopyListShowsTheCopiesOfTheSelectedPrint()
    {
        var api = new FakeItemApi();
        var medkit = api.AddItem("Medkit");
        api.AddCopy(medkit);
        var second = api.AddCopy(medkit);
        var screen = await SelectedScreenAsync(api);

        screen.SelectedPrint = screen.Prints.Single(print => print.PrintId == second.PrintId);

        Assert.Equal([second.Id], screen.Copies.Select(copy => copy.CopyId));
    }

    [Fact]
    public async Task ReprintWholePrintSendsEveryCopyOfThePrint()
    {
        var api = new FakeItemApi();
        var medkit = api.AddItem("Medkit");
        var printId = Guid.NewGuid();
        var copies = new[] { api.AddCopy(medkit, printId: printId), api.AddCopy(medkit, printId: printId) };
        api.AddCopy(medkit);
        var screen = await SelectedScreenAsync(api);
        screen.SelectedPrint = screen.Prints.Single(print => print.PrintId == printId);

        await screen.ReprintPrintCommand.ExecuteAsync();

        Assert.Equal(copies.Select(copy => copy.Id),
            JsonDocument.Parse(api.Submissions.Single().Body!).RootElement.GetProperty("copyIds").EnumerateArray().Select(id => id.GetGuid()));
    }

    [Fact]
    public async Task NewPrintIsSelectedAfterPrinting()
    {
        var api = new FakeItemApi();
        var medkit = api.AddItem("Medkit");
        api.AddCopy(medkit);
        var screen = await SelectedScreenAsync(api);

        await screen.PrintCommand.ExecuteAsync();

        Assert.Equal(PrintId(api.Submissions.Single().Body), screen.SelectedPrint!.PrintId);
    }

    [Fact]
    public async Task DeactivatedCopyIsShownAsDeactivated()
    {
        var api = new FakeItemApi();
        api.AddCopy(api.AddItem("Medkit"));
        var screen = await SelectedScreenAsync(api);
        screen.SelectedCopy = screen.Copies[0];

        await screen.ToggleCopyActiveCommand.ExecuteAsync();

        Assert.False(screen.Copies.Single().IsActive);
    }

    [Fact]
    public async Task ReaderCannotChangeItems()
    {
        var api = new FakeItemApi { CanWrite = false };
        api.AddItem("Medkit");

        var screen = await LoadedScreenAsync(api);

        Assert.False(screen.CanWrite);
    }

    [Fact]
    public async Task ItalianLabelsAreUsed()
    {
        var screen = CreateScreen(new FakeItemApi(), locale: "it");

        await screen.LoadAsync();

        Assert.Equal(("Stampa cartellini", "Tutti i tipi"), (screen.PrintLabel, screen.TypeFilters[0].Name));
    }

    [Fact]
    public async Task ManagingRaritiesOpensTheRarityCatalog()
    {
        var api = new FakeItemApi();
        api.AddRarity("Common");
        ItemCatalogViewModel? shown = null;
        var screen = await LoadedScreenAsync(api, new FakeItemDialogs { OnCatalog = catalog => Task.FromResult(shown = catalog) });

        await screen.ManageRaritiesCommand.ExecuteAsync();

        Assert.Equal((ItemCatalogKind.Rarities, "Common"), (shown!.Kind, shown.Entries.Single().Name));
    }

    private static (Guid, int)[] SentComponents(FakeItemApi api) =>
    [
        .. JsonDocument.Parse(api.Submissions.Single(submission => submission.ActionName == "create-item").Body!).RootElement
            .GetProperty("components").EnumerateArray()
            .Select(component => (component.GetProperty("itemId").GetGuid(), component.GetProperty("quantity").GetInt32()))
    ];

    private static Guid PrintId(string? body) => JsonDocument.Parse(body!).RootElement.GetProperty("printId").GetGuid();

    private static async Task<ItemScreenViewModel> LoadedScreenAsync(FakeItemApi api, FakeItemDialogs? dialogs = null)
    {
        var screen = CreateScreen(api, dialogs);
        await screen.LoadAsync();
        return screen;
    }

    private static async Task<ItemScreenViewModel> SelectedScreenAsync(FakeItemApi api, FakeItemDialogs? dialogs = null)
    {
        var screen = await LoadedScreenAsync(api, dialogs);
        screen.SelectedItem = screen.Items[0];
        screen.SelectedItems.Add(screen.Items[0]);
        return screen;
    }

    private static ItemScreenViewModel CreateScreen(FakeItemApi api, FakeItemDialogs? dialogs = null, string locale = "en") =>
        new(api, new JsonLocalizer(), dialogs ?? new FakeItemDialogs(), "http://localhost:5000/api", locale, FakeItemApi.ItemsHref);
}
