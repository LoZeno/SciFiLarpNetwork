using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.Skills;
using SciFiLarpNetwork.Management.Views;
using SciFiLarpNetwork.Management.Views.Skills;

namespace SciFiLarpNetwork.Management.Tests.Views;

public sealed class TalentCatalogWindowTests
{
    [AvaloniaFact]
    public async Task NewSkillOpensTheSkillDialogOverTheOwner()
    {
        var owner = ShowOwner();
        var catalog = CreateCatalog(new WindowCatalogDialogService(() => owner));
        await catalog.LoadAsync();

        var running = catalog.NewSkillCommand.ExecuteAsync();
        Dispatcher.UIThread.RunJobs();
        var dialog = owner.OwnedWindows.Single();
        var title = dialog.Title;
        dialog.Close();
        await running;

        Assert.Equal(new { Type = typeof(SkillEditorWindow), Title = (string?)"New skill" }, new { Type = dialog.GetType(), Title = title });
    }

    [AvaloniaFact]
    public async Task CancellingTheTalentDialogClosesIt()
    {
        var owner = ShowOwner();
        var editor = new TalentEditorViewModel(new JsonLocalizer(), "en", "skills.newTalent", _ => Task.FromResult(true));
        var showing = new WindowCatalogDialogService(() => owner).ShowTalentEditorAsync(editor);
        Dispatcher.UIThread.RunJobs();

        editor.CancelCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        Assert.True(showing.IsCompleted);
        await showing;
    }

    [AvaloniaFact]
    public async Task SavingTheSkillDialogClosesIt()
    {
        var owner = ShowOwner();
        var catalog = CreateCatalog(new WindowCatalogDialogService(() => owner));
        await catalog.LoadAsync();
        var running = catalog.NewSkillCommand.ExecuteAsync();
        Dispatcher.UIThread.RunJobs();
        var editor = (SkillEditorViewModel)owner.OwnedWindows.Single().DataContext!;
        editor.Name = "First aid";

        await editor.SaveCommand.ExecuteAsync();
        Dispatcher.UIThread.RunJobs();
        await running;

        Assert.Empty(owner.OwnedWindows);
    }

    [AvaloniaTheory]
    [InlineData("Archive", true)]
    [InlineData("Cancel", false)]
    public async Task ConfirmationReturnsTheChosenButton(string buttonText, bool expected)
    {
        var owner = ShowOwner();
        var asking = new WindowCatalogDialogService(() => owner)
            .ConfirmAsync(new ConfirmationRequest("Please confirm", "Archive?", "Archive", "Cancel"));
        Dispatcher.UIThread.RunJobs();
        var button = owner.OwnedWindows.Single().GetVisualDescendants().OfType<Button>()
            .Single(candidate => Equals(candidate.Content, buttonText));

        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(expected, await asking);
    }

    [AvaloniaFact]
    public async Task MainWindowShowsTheTalentsScreenWhenItIsOpen()
    {
        var api = new FakeCatalogApi();
        api.AddTalent("Medicine");
        var viewModel = new MainWindowViewModel(
            api,
            new NullSettingsStore(),
            new JsonLocalizer(),
            new FakeCatalogDialogs(),
            new ManagementSettings());
        var window = new MainWindow(viewModel);
        window.Show();
        await viewModel.ConnectCommand.ExecuteAsync();
        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];

        await viewModel.OpenResourceCommand.ExecuteAsync();
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.GetVisualDescendants().OfType<TalentCatalogView>().Single().IsEffectivelyVisible);
        window.Close();
    }

    private static Window ShowOwner()
    {
        var owner = new Window();
        owner.Show();
        return owner;
    }

    private static TalentCatalogViewModel CreateCatalog(ICatalogDialogService dialogs) =>
        new(new FakeCatalogApi(), new JsonLocalizer(), dialogs, "http://localhost:5000/api", "en", FakeCatalogApi.TalentsHref);

    private sealed class NullSettingsStore : IManagementSettingsStore
    {
        public ManagementSettings Load() => new();

        public void Save(ManagementSettings settings)
        {
        }
    }
}
