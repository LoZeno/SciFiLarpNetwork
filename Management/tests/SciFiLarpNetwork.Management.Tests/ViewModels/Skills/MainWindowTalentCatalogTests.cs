using System.Net;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.ViewModels;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Skills;

public sealed class MainWindowTalentCatalogTests
{
    [Fact]
    public async Task ConnectRecognizesTheTalentsLink()
    {
        var viewModel = CreateViewModel(new FakeCatalogApi());

        await viewModel.ConnectCommand.ExecuteAsync();

        Assert.Equal([new NavigationItem("Talents", FakeCatalogApi.TalentsHref, true)], viewModel.NavigationItems);
    }

    [Fact]
    public async Task OpeningTheTalentsLinkShowsTheTalentsScreen()
    {
        var api = new FakeCatalogApi();
        api.AddTalent("Medicine");
        var viewModel = await OpenCatalogAsync(api);

        Assert.Equal(
            new { IsOpen = true, IsSummaryVisible = false, Talents = "Medicine" },
            new
            {
                IsOpen = viewModel.IsTalentCatalogOpen,
                IsSummaryVisible = viewModel.IsResourceSummaryVisible,
                Talents = string.Join(',', viewModel.TalentCatalog!.Talents.Select(talent => talent.Name))
            });
    }

    [Fact]
    public async Task ExpiredSessionInTheTalentsScreenSignsOut()
    {
        var api = new FakeCatalogApi();
        api.AddTalent("Medicine");
        var viewModel = await OpenCatalogAsync(api);
        await viewModel.SignInCommand.ExecuteAsync();
        api.GetFailure = new ManagementApiException("errors.unauthorized", HttpStatusCode.Unauthorized);

        viewModel.TalentCatalog!.ShowArchivedTalents = true;

        Assert.Equal(
            new { IsAuthenticated = false, IsOpen = false, Status = "Sign in to continue." },
            new { viewModel.IsAuthenticated, IsOpen = viewModel.IsTalentCatalogOpen, Status = viewModel.StatusMessage });
    }

    [Fact]
    public async Task LanguageChangeReachesTheTalentsScreen()
    {
        var viewModel = await OpenCatalogAsync(new FakeCatalogApi());

        viewModel.Locale = "it";

        Assert.Equal("Attitudini", viewModel.TalentCatalog!.TalentsHeading);
    }

    [Fact]
    public async Task ConnectingAgainClosesTheTalentsScreen()
    {
        var viewModel = await OpenCatalogAsync(new FakeCatalogApi());

        await viewModel.ConnectCommand.ExecuteAsync();

        Assert.False(viewModel.IsTalentCatalogOpen);
    }

    private static async Task<MainWindowViewModel> OpenCatalogAsync(FakeCatalogApi api)
    {
        var viewModel = CreateViewModel(api);
        await viewModel.ConnectCommand.ExecuteAsync();
        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        await viewModel.OpenResourceCommand.ExecuteAsync();
        return viewModel;
    }

    private static MainWindowViewModel CreateViewModel(FakeCatalogApi api) =>
        new(api, new NullSettingsStore(), new JsonLocalizer(), new FakeCatalogDialogs(), new ManagementSettings());

    private sealed class NullSettingsStore : IManagementSettingsStore
    {
        public ManagementSettings Load() => new();

        public void Save(ManagementSettings settings)
        {
        }
    }
}
