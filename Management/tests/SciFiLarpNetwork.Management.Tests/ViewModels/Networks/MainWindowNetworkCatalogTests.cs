using SciFiLarpNetwork.Management.Tests.ViewModels.CardTemplates;
using SciFiLarpNetwork.Management.Tests.ViewModels.LarpEvents;
using System.Net;
using SciFiLarpNetwork.Management.Tests.ViewModels.Players;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Networks;

public sealed class MainWindowNetworkCatalogTests
{
    [Fact]
    public async Task ConnectRecognizesTheNetworksLink()
    {
        var viewModel = CreateViewModel(new FakeNetworkApi());

        await viewModel.ConnectCommand.ExecuteAsync();

        Assert.Equal([new NavigationItem("Networks", FakeNetworkApi.NetworksHref, NavigationScreen.NetworkCatalog)], viewModel.NavigationItems);
    }

    [Fact]
    public async Task SelectingTheNetworksLinkShowsTheNetworksScreen()
    {
        var api = new FakeNetworkApi();
        api.AddNetwork("rim", "Outer Rim");
        var viewModel = CreateViewModel(api);
        await viewModel.ConnectCommand.ExecuteAsync();

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];

        Assert.Equal(
            new { IsNetworksOpen = true, IsTalentsOpen = false, IsSummaryVisible = false, Networks = "Outer Rim" },
            new
            {
                IsNetworksOpen = viewModel.IsNetworkCatalogOpen,
                IsTalentsOpen = viewModel.IsTalentCatalogOpen,
                IsSummaryVisible = viewModel.IsResourceSummaryVisible,
                Networks = string.Join(',', viewModel.NetworkCatalog!.Networks.Select(network => network.Name))
            });
    }

    [Fact]
    public async Task ExpiredSessionInTheNetworksScreenSignsOut()
    {
        var api = new FakeNetworkApi();
        var viewModel = CreateViewModel(api);
        await viewModel.SignInCommand.ExecuteAsync();
        await viewModel.ConnectCommand.ExecuteAsync();
        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        api.GetFailure = new ManagementApiException("errors.unauthorized", HttpStatusCode.Unauthorized);

        viewModel.NetworkCatalog!.ShowDisabled = true;

        Assert.Equal(
            new { IsAuthenticated = false, IsOpen = false },
            new { viewModel.IsAuthenticated, IsOpen = viewModel.IsNetworkCatalogOpen });
    }

    private static MainWindowViewModel CreateViewModel(FakeNetworkApi api) =>
        new(api, new NullSettingsStore(), new JsonLocalizer(), new FakeCatalogDialogs(), new FakeNetworkDialogs(), new FakePlayerDialogs(),
            new FakeCardTemplateDialogs(), new FakeLarpEventDialogs(), new ManagementSettings());

    private sealed class NullSettingsStore : IManagementSettingsStore
    {
        public ManagementSettings Load() => new();

        public void Save(ManagementSettings settings)
        {
        }
    }
}
