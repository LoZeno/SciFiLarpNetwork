using SciFiLarpNetwork.Management.Tests.ViewModels.News;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.Tests.ViewModels.CardTemplates;
using SciFiLarpNetwork.Management.Tests.ViewModels.LarpEvents;
using SciFiLarpNetwork.Management.Tests.ViewModels.Messages;
using SciFiLarpNetwork.Management.Tests.ViewModels.Networks;
using SciFiLarpNetwork.Management.Tests.ViewModels.Players;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.Messages;
using SciFiLarpNetwork.Management.Views;
using SciFiLarpNetwork.Management.Views.Messages;

namespace SciFiLarpNetwork.Management.Tests.Views;

public sealed class MessageWindowTests
{
    [Fact]
    public Task MainWindowShowsTheMessagesScreenWhenItsEntryIsSelected() => AvaloniaUi.Run(async () =>
    {
        var api = new FakeMessageApi();
        api.AddMessage("Supply run", FakeMessageApi.KiraId, FakeMessageApi.OdoId);
        var viewModel = CreateViewModel(api);
        var window = new MainWindow(viewModel);
        window.Show();
        await viewModel.ConnectCommand.ExecuteAsync();

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.GetVisualDescendants().OfType<MessageScreenView>().Single().IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public Task OpenedMessageShowsItsTextInAReadOnlyBox() => AvaloniaUi.Run(async () =>
    {
        var api = new FakeMessageApi();
        api.Messages.Add(new FakeMessage(Guid.NewGuid(), FakeMessageApi.KiraId, [FakeMessageApi.OdoId], "Supply run",
            "Meet at the dock.\r\nBring crates.", DateTimeOffset.UnixEpoch, null));
        var viewModel = CreateViewModel(api);
        var window = new MainWindow(viewModel);
        window.Show();
        await viewModel.ConnectCommand.ExecuteAsync();
        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];

        viewModel.Messages!.SelectedMessage = viewModel.Messages.Messages[0];
        Dispatcher.UIThread.RunJobs();

        var body = window.GetVisualDescendants().OfType<MessageScreenView>().Single()
            .GetVisualDescendants().OfType<TextBox>().Single(box => box.IsReadOnly);
        Assert.Equal(new { Text = (string?)"Meet at the dock.\r\nBring crates.", Visible = true }, new { body.Text, Visible = body.IsEffectivelyVisible });
        window.Close();
    });

    [Fact]
    public Task MessageWindowListsTheReplyRecipient() => AvaloniaUi.Run(async () =>
    {
        var api = new FakeMessageApi();
        api.AddMessage("Supply run", FakeMessageApi.KiraId, FakeMessageApi.OdoId);
        MessageEditorWindow? shown = null;
        var dialogs = new FakeMessageDialogs
        {
            OnMessageEditor = editor =>
            {
                shown = new MessageEditorWindow(editor);
                shown.Show();
                Dispatcher.UIThread.RunJobs();
                return Task.CompletedTask;
            }
        };
        var screen = new MessageScreenViewModel(api, new JsonLocalizer(), dialogs, "http://localhost/api", "en", FakeMessageApi.MessagesHref);
        await screen.LoadAsync();
        screen.SelectedMessage = screen.Messages[0];

        await screen.ReplyCommand.ExecuteAsync();

        var recipients = shown!.GetVisualDescendants().OfType<ListBox>()
            .Single(list => Equals(list.ItemsSource, dialogs.LastEditor!.Recipients));
        Assert.Equal(["#1 Kira"], recipients.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text));
        shown?.Close();
    });

    private static MainWindowViewModel CreateViewModel(FakeMessageApi api) =>
        new(api, new NullSettingsStore(), new JsonLocalizer(), new FakeCatalogDialogs(), new FakeNetworkDialogs(), new FakePlayerDialogs(),
            new FakeCardTemplateDialogs(), new FakeLarpEventDialogs(), new FakeMessageDialogs(), new FakeNewsDialogs(), new SciFiLarpNetwork.Management.Tests.ViewModels.Items.FakeItemDialogs(), new ManagementSettings());

    private sealed class NullSettingsStore : IManagementSettingsStore
    {
        public ManagementSettings Load() => new();

        public void Save(ManagementSettings settings)
        {
        }
    }
}
