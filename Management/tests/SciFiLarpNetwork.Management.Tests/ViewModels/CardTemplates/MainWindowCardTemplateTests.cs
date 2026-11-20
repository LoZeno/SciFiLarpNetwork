using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.Tests.ViewModels.Networks;
using SciFiLarpNetwork.Management.Tests.ViewModels.Players;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.CardTemplates;

public sealed class MainWindowCardTemplateTests
{
    [Fact]
    public async Task ConnectRecognizesTheCardTemplatesLink()
    {
        var viewModel = CreateViewModel(new FakeCardTemplateApi());

        await viewModel.ConnectCommand.ExecuteAsync();

        Assert.Equal(
            [new NavigationItem("Card templates", FakeCardTemplateApi.TemplatesHref, NavigationScreen.CardTemplates)],
            viewModel.NavigationItems);
    }

    [Fact]
    public async Task SelectingTheCardTemplatesLinkShowsTheScreen()
    {
        var viewModel = CreateViewModel(new FakeCardTemplateApi());
        await viewModel.ConnectCommand.ExecuteAsync();

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];

        Assert.Equal(
            new { IsOpen = true, IsSummaryVisible = false, State = (string?)"Default" },
            new
            {
                IsOpen = viewModel.IsCardTemplatesOpen,
                IsSummaryVisible = viewModel.IsResourceSummaryVisible,
                State = viewModel.CardTemplates?.CharacterTemplateState
            });
    }

    private static MainWindowViewModel CreateViewModel(FakeCardTemplateApi api) =>
        new(api, new NullSettingsStore(), new JsonLocalizer(), new FakeCatalogDialogs(), new FakeNetworkDialogs(), new FakePlayerDialogs(),
            new FakeCardTemplateDialogs(), new ManagementSettings());

    private sealed class NullSettingsStore : IManagementSettingsStore
    {
        public ManagementSettings Load() => new();

        public void Save(ManagementSettings settings)
        {
        }
    }
}
