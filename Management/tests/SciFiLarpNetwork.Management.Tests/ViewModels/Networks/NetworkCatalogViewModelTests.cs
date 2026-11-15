using System.Net;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.Networks;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Networks;

public sealed class NetworkCatalogViewModelTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01];

    [Fact]
    public async Task LoadListsEnabledNetworksWithTheirCodeAndDefaultTag()
    {
        var api = new FakeNetworkApi();
        api.AddNetwork("core", "Core", isDefault: true);
        api.AddNetwork("rim", "Outer Rim");
        api.AddNetwork("old", "Old Net", isEnabled: false);
        var catalog = CreateCatalog(api);

        await catalog.LoadAsync();

        Assert.Equal(["Core [core] (Default)", "Outer Rim [rim]"], catalog.Networks.Select(network => network.DisplayName));
    }

    [Fact]
    public async Task ShowingDisabledNetworksMarksThem()
    {
        var api = new FakeNetworkApi();
        api.AddNetwork("old", "Old Net", isEnabled: false);
        var catalog = CreateCatalog(api);
        await catalog.LoadAsync();

        catalog.ShowDisabled = true;

        Assert.Equal(["Old Net [old] (Disabled)"], catalog.Networks.Select(network => network.DisplayName));
    }

    [Theory]
    [InlineData("rim", "Outer Rim")]
    [InlineData("OUTER", "Outer Rim")]
    [InlineData("cor", "Core")]
    public async Task SearchMatchesNamesAndCodes(string search, string expectedName)
    {
        var api = new FakeNetworkApi();
        api.AddNetwork("core", "Core");
        api.AddNetwork("rim", "Outer Rim");
        var catalog = CreateCatalog(api);
        await catalog.LoadAsync();

        catalog.SearchText = search;

        Assert.Equal([expectedName], catalog.Networks.Select(network => network.Name));
    }

    [Fact]
    public async Task SearchWithoutMatchesShowsTheNoResultsText()
    {
        var api = new FakeNetworkApi();
        api.AddNetwork("core", "Core");
        var catalog = CreateCatalog(api);
        await catalog.LoadAsync();

        catalog.SearchText = "nothing";

        Assert.Equal("No results found.", catalog.NetworksPlaceholder);
    }

    [Fact]
    public async Task MissingDefaultShowsAWarning()
    {
        var api = new FakeNetworkApi();
        api.AddNetwork("rim", "Outer Rim");
        var catalog = CreateCatalog(api);

        await catalog.LoadAsync();

        Assert.True(catalog.HasNoDefaultWarning);
    }

    [Fact]
    public async Task ExistingDefaultHidesTheWarning()
    {
        var api = new FakeNetworkApi();
        api.AddNetwork("rim", "Outer Rim", isDefault: true);
        var catalog = CreateCatalog(api);

        await catalog.LoadAsync();

        Assert.False(catalog.HasNoDefaultWarning);
    }

    [Fact]
    public async Task ReadersCannotChangeTheCatalog()
    {
        var catalog = CreateCatalog(new FakeNetworkApi { CanWrite = false });

        await catalog.LoadAsync();

        Assert.False(catalog.CanWrite);
    }

    [Fact]
    public async Task TheDefaultNetworkCannotBeDisabledOrMadeDefaultAgain()
    {
        var api = new FakeNetworkApi();
        api.AddNetwork("rim", "Outer Rim", isDefault: true);
        var catalog = await LoadWithSelectionAsync(api);

        Assert.Equal(
            new { CanToggle = false, CanMakeDefault = false, CanEdit = true },
            new
            {
                CanToggle = catalog.ToggleEnabledCommand.CanExecute(null),
                CanMakeDefault = catalog.MakeDefaultCommand.CanExecute(null),
                CanEdit = catalog.EditNetworkCommand.CanExecute(null)
            });
    }

    [Fact]
    public async Task DisabledNetworkOffersEnableButNotMakeDefault()
    {
        var api = new FakeNetworkApi();
        api.AddNetwork("old", "Old Net", isEnabled: false);
        var catalog = CreateCatalog(api);
        await catalog.LoadAsync();
        catalog.ShowDisabled = true;

        catalog.SelectedNetwork = catalog.Networks[0];

        Assert.Equal(
            new { Label = "Enable", CanMakeDefault = false },
            new { Label = catalog.ToggleEnabledLabel, CanMakeDefault = catalog.MakeDefaultCommand.CanExecute(null) });
    }

    [Fact]
    public async Task NewNetworkSendsCodeNameDescriptionAndTheme()
    {
        var api = new FakeNetworkApi();
        var dialogs = new FakeNetworkDialogs
        {
            OnNetworkEditor = editor =>
            {
                editor.Code = "rim";
                editor.Name = "Outer Rim";
                editor.Description = "Frontier relay";
                editor.SelectedTheme = editor.Themes.Single(theme => theme.Name == "grey");
                return editor.SaveCommand.ExecuteAsync();
            }
        };
        var catalog = CreateCatalog(api, dialogs);
        await catalog.LoadAsync();

        await catalog.NewNetworkCommand.ExecuteAsync();

        Assert.Equal(
            new Submission("create-network", """{"code":"rim","name":"Outer Rim","description":"Frontier relay","theme":"grey"}"""),
            api.Submissions.Single());
    }

    [Fact]
    public async Task NewNetworkDialogOffersEveryTheme()
    {
        var dialogs = new FakeNetworkDialogs();
        var catalog = CreateCatalog(new FakeNetworkApi(), dialogs);
        await catalog.LoadAsync();

        await catalog.NewNetworkCommand.ExecuteAsync();

        Assert.Equal(7, dialogs.LastEditor!.Themes.Count);
    }

    [Fact]
    public async Task NewNetworkWithAnImageUploadsItAfterCreating()
    {
        var api = new FakeNetworkApi();
        var dialogs = new FakeNetworkDialogs
        {
            PickedImage = Png,
            OnNetworkEditor = async editor =>
            {
                editor.Code = "rim";
                editor.Name = "Outer Rim";
                await editor.ChooseImageCommand.ExecuteAsync();
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var catalog = CreateCatalog(api, dialogs);
        await catalog.LoadAsync();

        await catalog.NewNetworkCommand.ExecuteAsync();

        Assert.Equal(Png, api.Images[api.Networks.Single().ImageId!.Value]);
    }

    [Fact]
    public async Task FailedImageUploadKeepsTheCreatedNetworkAndDoesNotCreateItTwice()
    {
        var api = new FakeNetworkApi();
        api.SubmitFailures["set-background-image"] = new ManagementApiException("errors.invalidImage", HttpStatusCode.BadRequest);
        var dialogs = new FakeNetworkDialogs
        {
            PickedImage = Png,
            OnNetworkEditor = async editor =>
            {
                editor.Code = "rim";
                editor.Name = "Outer Rim";
                await editor.ChooseImageCommand.ExecuteAsync();
                await editor.SaveCommand.ExecuteAsync();
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var catalog = CreateCatalog(api, dialogs);
        await catalog.LoadAsync();

        await catalog.NewNetworkCommand.ExecuteAsync();

        Assert.Equal(
            ["create-network", "set-background-image", "update-network", "set-background-image"],
            api.Submissions.Select(submission => submission.ActionName));
    }

    [Fact]
    public async Task RejectedImageIsShownInTheDialog()
    {
        var api = new FakeNetworkApi();
        api.SubmitFailures["set-background-image"] = new ManagementApiException("errors.invalidImage", HttpStatusCode.BadRequest);
        var dialogs = new FakeNetworkDialogs
        {
            PickedImage = Png,
            OnNetworkEditor = async editor =>
            {
                editor.Code = "rim";
                editor.Name = "Outer Rim";
                await editor.ChooseImageCommand.ExecuteAsync();
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var catalog = CreateCatalog(api, dialogs);
        await catalog.LoadAsync();

        await catalog.NewNetworkCommand.ExecuteAsync();

        Assert.Equal("Upload a PNG, JPEG, or WebP image.", dialogs.LastEditor!.ErrorMessage);
    }

    [Fact]
    public async Task TooLargeImageIsRejectedBeforeUploading()
    {
        var dialogs = new FakeNetworkDialogs { PickedImage = new byte[NetworkEditorViewModel.MaximumImageByteCount + 1] };
        var catalog = CreateCatalog(new FakeNetworkApi(), dialogs);
        await catalog.LoadAsync();
        await catalog.NewNetworkCommand.ExecuteAsync();

        await dialogs.LastEditor!.ChooseImageCommand.ExecuteAsync();

        Assert.Equal(
            new { Error = (string?)"The image must be 2 MB or smaller.", HasImage = false },
            new { Error = dialogs.LastEditor.ErrorMessage, HasImage = dialogs.LastEditor.HasPreviewImage });
    }

    [Fact]
    public async Task CancelledImagePickerKeepsTheCurrentImage()
    {
        var api = new FakeNetworkApi();
        api.AddImage(api.AddNetwork("rim", "Outer Rim"), Png);
        var dialogs = new FakeNetworkDialogs { PickedImage = null };
        var catalog = await LoadWithSelectionAsync(api, dialogs);
        await catalog.EditNetworkCommand.ExecuteAsync();

        await dialogs.LastEditor!.ChooseImageCommand.ExecuteAsync();

        Assert.Equal(Png, dialogs.LastEditor.PreviewImage);
    }

    [Fact]
    public async Task EditShowsTheSavedImage()
    {
        var api = new FakeNetworkApi();
        api.AddImage(api.AddNetwork("rim", "Outer Rim"), Png);
        var dialogs = new FakeNetworkDialogs();
        var catalog = await LoadWithSelectionAsync(api, dialogs);

        await catalog.EditNetworkCommand.ExecuteAsync();

        Assert.Equal(Png, dialogs.LastEditor!.PreviewImage);
    }

    [Fact]
    public async Task EditStartsFromTheSavedDetailsWithAFixedCode()
    {
        var api = new FakeNetworkApi();
        var network = api.AddNetwork("rim", "Outer Rim");
        network.Theme = "purple";
        var dialogs = new FakeNetworkDialogs();
        var catalog = await LoadWithSelectionAsync(api, dialogs);

        await catalog.EditNetworkCommand.ExecuteAsync();

        Assert.Equal(
            new { Code = "rim", Name = "Outer Rim", Theme = (string?)"purple", CodeEditable = false },
            new
            {
                dialogs.LastEditor!.Code,
                dialogs.LastEditor.Name,
                Theme = dialogs.LastEditor.SelectedTheme?.Name,
                CodeEditable = dialogs.LastEditor.IsCodeEditable
            });
    }

    [Fact]
    public async Task EditSendsTheVersionWithoutTheCode()
    {
        var api = new FakeNetworkApi();
        api.AddNetwork("rim", "Outer Rim").Version = 3;
        var dialogs = new FakeNetworkDialogs
        {
            OnNetworkEditor = editor =>
            {
                editor.Name = "Far Rim";
                return editor.SaveCommand.ExecuteAsync();
            }
        };
        var catalog = await LoadWithSelectionAsync(api, dialogs);

        await catalog.EditNetworkCommand.ExecuteAsync();

        Assert.Equal(new Submission("update-network", """{"version":3,"name":"Far Rim","theme":"blue"}"""), api.Submissions.Single());
    }

    [Fact]
    public async Task RemovingTheImageSendsTheRemovalAfterTheDetails()
    {
        var api = new FakeNetworkApi();
        var network = api.AddNetwork("rim", "Outer Rim");
        api.AddImage(network, Png);
        var dialogs = new FakeNetworkDialogs
        {
            OnNetworkEditor = editor =>
            {
                editor.RemoveImageCommand.Execute(null);
                return editor.SaveCommand.ExecuteAsync();
            }
        };
        var catalog = await LoadWithSelectionAsync(api, dialogs);

        await catalog.EditNetworkCommand.ExecuteAsync();

        Assert.Equal(
            new { Actions = "update-network,remove-background-image", ImageId = (Guid?)null },
            new { Actions = string.Join(',', api.Submissions.Select(submission => submission.ActionName)), network.ImageId });
    }

    [Fact]
    public async Task StaleEditReloadsTheLatestValuesIntoTheDialog()
    {
        var api = new FakeNetworkApi();
        var network = api.AddNetwork("rim", "Outer Rim");
        var dialogs = new FakeNetworkDialogs
        {
            OnNetworkEditor = editor =>
            {
                network.Name = "Changed Elsewhere";
                network.Version = 2;
                editor.Name = "Far Rim";
                return editor.SaveCommand.ExecuteAsync();
            }
        };
        var catalog = await LoadWithSelectionAsync(api, dialogs);

        await catalog.EditNetworkCommand.ExecuteAsync();

        Assert.Equal(
            new { Name = "Changed Elsewhere", Error = (string?)"Someone else changed this item. Reload it and try again." },
            new { dialogs.LastEditor!.Name, Error = dialogs.LastEditor.ErrorMessage });
    }

    [Fact]
    public async Task DuplicateCodeIsShownInTheDialog()
    {
        var api = new FakeNetworkApi();
        api.SubmitFailures["create-network"] = new ManagementApiException("errors.codeAlreadyUsed", HttpStatusCode.Conflict);
        var dialogs = new FakeNetworkDialogs
        {
            OnNetworkEditor = editor =>
            {
                editor.Code = "rim";
                editor.Name = "Outer Rim";
                return editor.SaveCommand.ExecuteAsync();
            }
        };
        var catalog = CreateCatalog(api, dialogs);
        await catalog.LoadAsync();

        await catalog.NewNetworkCommand.ExecuteAsync();

        Assert.Equal("This code is already used.", dialogs.LastEditor!.ErrorMessage);
    }

    [Fact]
    public async Task SavedNetworkAppearsInTheList()
    {
        var api = new FakeNetworkApi();
        var dialogs = new FakeNetworkDialogs
        {
            OnNetworkEditor = editor =>
            {
                editor.Code = "rim";
                editor.Name = "Outer Rim";
                return editor.SaveCommand.ExecuteAsync();
            }
        };
        var catalog = CreateCatalog(api, dialogs);
        await catalog.LoadAsync();

        await catalog.NewNetworkCommand.ExecuteAsync();

        Assert.Equal(["Outer Rim"], catalog.Networks.Select(network => network.Name));
    }

    [Fact]
    public async Task DisablingAsksForConfirmationByName()
    {
        var api = new FakeNetworkApi();
        api.AddNetwork("rim", "Outer Rim");
        var dialogs = new FakeNetworkDialogs { ConfirmResult = false };
        var catalog = await LoadWithSelectionAsync(api, dialogs);

        await catalog.ToggleEnabledCommand.ExecuteAsync();

        Assert.Equal(
            new ConfirmationRequest(
                "Please confirm",
                "Disable the network \"Outer Rim\"? Characters and history keep it, but it cannot be chosen for new assignments.",
                "Disable",
                "Cancel"),
            dialogs.LastConfirmation);
    }

    [Fact]
    public async Task DeclinedDisableSendsNothing()
    {
        var api = new FakeNetworkApi();
        api.AddNetwork("rim", "Outer Rim");
        var catalog = await LoadWithSelectionAsync(api, new FakeNetworkDialogs { ConfirmResult = false });

        await catalog.ToggleEnabledCommand.ExecuteAsync();

        Assert.Empty(api.Submissions);
    }

    [Fact]
    public async Task ConfirmedDisableHidesTheNetwork()
    {
        var api = new FakeNetworkApi();
        api.AddNetwork("rim", "Outer Rim");
        var catalog = await LoadWithSelectionAsync(api, new FakeNetworkDialogs());

        await catalog.ToggleEnabledCommand.ExecuteAsync();

        Assert.Empty(catalog.Networks);
    }

    [Fact]
    public async Task MakingADefaultMovesTheTagAndClearsTheWarning()
    {
        var api = new FakeNetworkApi();
        api.AddNetwork("core", "Core");
        var catalog = await LoadWithSelectionAsync(api, new FakeNetworkDialogs());

        await catalog.MakeDefaultCommand.ExecuteAsync();

        Assert.Equal(
            new { Name = "Core [core] (Default)", Warning = false },
            new { Name = catalog.Networks.Single().DisplayName, Warning = catalog.HasNoDefaultWarning });
    }

    [Fact]
    public async Task ExpiredSessionIsReported()
    {
        var catalog = CreateCatalog(new FakeNetworkApi { GetFailure = new ManagementApiException("errors.unauthorized", HttpStatusCode.Unauthorized) });
        var expired = false;
        catalog.SessionExpired += (_, _) => expired = true;

        await catalog.LoadAsync();

        Assert.True(expired);
    }

    [Fact]
    public async Task LoadFailureIsShownAsStatus()
    {
        var catalog = CreateCatalog(new FakeNetworkApi { GetFailure = new HttpRequestException() });

        await catalog.LoadAsync();

        Assert.Equal("The network service is unavailable", catalog.StatusMessage);
    }

    [Fact]
    public void ChangingTheLanguageRelabelsTheScreen()
    {
        var api = new FakeNetworkApi();
        api.AddNetwork("core", "Core", isDefault: true);
        var catalog = CreateCatalog(api);

        catalog.Locale = "it";

        Assert.Equal(
            new { Heading = "Reti", Network = "Core [core] (Predefinita)" },
            new { Heading = catalog.NetworksHeading, Network = catalog.Networks.Single().DisplayName });
    }

    private static NetworkCatalogViewModel CreateCatalog(FakeNetworkApi api, FakeNetworkDialogs? dialogs = null) =>
        new(api, new JsonLocalizer(), dialogs ?? new FakeNetworkDialogs(), "http://localhost:5000/api", "en", FakeNetworkApi.NetworksHref);

    private static async Task<NetworkCatalogViewModel> LoadWithSelectionAsync(FakeNetworkApi api, FakeNetworkDialogs? dialogs = null)
    {
        var catalog = CreateCatalog(api, dialogs);
        await catalog.LoadAsync();
        catalog.SelectedNetwork = catalog.Networks[0];
        return catalog;
    }
}
