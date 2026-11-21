using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.Tests.ViewModels.CardTemplates;
using SciFiLarpNetwork.Management.Tests.ViewModels.Networks;
using SciFiLarpNetwork.Management.Tests.ViewModels.Players;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.LarpEvents;

public sealed class MainWindowLarpEventTests
{
    [Fact]
    public async Task ConnectRecognizesTheEventsLink()
    {
        var viewModel = CreateViewModel(new FakeLarpEventApi());

        await viewModel.ConnectCommand.ExecuteAsync();

        Assert.Equal([new NavigationItem("Events", FakeLarpEventApi.EventsHref, NavigationScreen.Events)], viewModel.NavigationItems);
    }

    [Fact]
    public async Task SelectingTheEventsLinkShowsTheScreen()
    {
        var api = new FakeLarpEventApi();
        api.AddEvent("Fort");
        var viewModel = CreateViewModel(api);
        await viewModel.ConnectCommand.ExecuteAsync();

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];

        Assert.Equal(
            new { IsOpen = true, IsSummaryVisible = false, Count = (int?)1 },
            new { IsOpen = viewModel.IsEventsOpen, IsSummaryVisible = viewModel.IsResourceSummaryVisible, Count = viewModel.Events?.Events.Count });
    }

    private static MainWindowViewModel CreateViewModel(FakeLarpEventApi api) =>
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
