using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.Tests.ViewModels.CardTemplates;
using SciFiLarpNetwork.Management.Tests.ViewModels.LarpEvents;
using SciFiLarpNetwork.Management.Tests.ViewModels.Networks;
using SciFiLarpNetwork.Management.Tests.ViewModels.Players;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Messages;

public sealed class MainWindowMessageTests
{
    [Fact]
    public async Task ConnectRecognizesTheMessagesLink()
    {
        var viewModel = CreateViewModel(new FakeMessageApi());

        await viewModel.ConnectCommand.ExecuteAsync();

        Assert.Equal([new NavigationItem("Messages", FakeMessageApi.MessagesHref, NavigationScreen.Messages)], viewModel.NavigationItems);
    }

    [Fact]
    public async Task SelectingTheMessagesLinkShowsTheScreen()
    {
        var api = new FakeMessageApi();
        api.AddMessage("Supply run", FakeMessageApi.KiraId, FakeMessageApi.OdoId);
        var viewModel = CreateViewModel(api);
        await viewModel.ConnectCommand.ExecuteAsync();

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];

        Assert.Equal(
            new { IsOpen = true, IsSummaryVisible = false, Count = (int?)1 },
            new { IsOpen = viewModel.IsMessagesOpen, IsSummaryVisible = viewModel.IsResourceSummaryVisible, Count = viewModel.Messages?.Messages.Count });
    }

    private static MainWindowViewModel CreateViewModel(FakeMessageApi api) =>
        new(api, new NullSettingsStore(), new JsonLocalizer(), new FakeCatalogDialogs(), new FakeNetworkDialogs(), new FakePlayerDialogs(),
            new FakeCardTemplateDialogs(), new FakeLarpEventDialogs(), new FakeMessageDialogs(), new ManagementSettings());

    private sealed class NullSettingsStore : IManagementSettingsStore
    {
        public ManagementSettings Load() => new();

        public void Save(ManagementSettings settings)
        {
        }
    }
}
