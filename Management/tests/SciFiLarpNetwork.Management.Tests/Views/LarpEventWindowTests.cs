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
using SciFiLarpNetwork.Management.ViewModels.LarpEvents;
using SciFiLarpNetwork.Management.Views;
using SciFiLarpNetwork.Management.Views.LarpEvents;

namespace SciFiLarpNetwork.Management.Tests.Views;

public sealed class LarpEventWindowTests
{
    private static readonly DateOnly FirstDay = new(2026, 5, 1);

    [Fact]
    public Task MainWindowShowsTheEventsScreenWhenItsEntryIsSelected() => AvaloniaUi.Run(async () =>
    {
        var api = new FakeLarpEventApi();
        api.AddEvent("Fort", true, FirstDay);
        var viewModel = CreateViewModel(api, new FakeLarpEventDialogs());
        var window = new MainWindow(viewModel);
        window.Show();
        await viewModel.ConnectCommand.ExecuteAsync();

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.GetVisualDescendants().OfType<LarpEventScreenView>().Single().IsEffectivelyVisible);
        window.Close();
    });

    [Fact]
    public Task ClosedEventShowsItsExplanation() => AvaloniaUi.Run(async () =>
    {
        var api = new FakeLarpEventApi();
        api.AddEvent("Fort", isOpen: false, FirstDay);
        var viewModel = CreateViewModel(api, new FakeLarpEventDialogs());
        var window = new MainWindow(viewModel);
        window.Show();
        await viewModel.ConnectCommand.ExecuteAsync();
        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];

        var screen = viewModel.Events!;
        screen.SelectedEvent = screen.Events[0];
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(),
            text => text.IsEffectivelyVisible && text.Text == screen.ClosedExplanation);
        window.Close();
    });

    [Fact]
    public Task RegistrationWindowListsTheEventDays() => AvaloniaUi.Run(async () =>
    {
        var api = new FakeLarpEventApi();
        api.AddEvent("Fort", true, FirstDay, FirstDay.AddDays(1));
        RegistrationWindow? shown = null;
        var dialogs = new FakeLarpEventDialogs
        {
            OnRegistration = editor =>
            {
                shown = new RegistrationWindow(editor);
                shown.Show();
                Dispatcher.UIThread.RunJobs();
                return Task.CompletedTask;
            }
        };
        var screen = new LarpEventScreenViewModel(api, new JsonLocalizer(), dialogs, "http://localhost/api", "en", FakeLarpEventApi.EventsHref);
        await screen.LoadAsync();
        screen.SelectedEvent = screen.Events[0];

        await screen.RegisterCommand.ExecuteAsync();

        Assert.Equal(
            ["2026-05-01", "2026-05-02"],
            shown!.GetVisualDescendants().OfType<CheckBox>().Select(box => box.Content as string));
        shown?.Close();
    });

    [Fact]
    public Task DayWindowShowsTheDayBeingEdited() => AvaloniaUi.Run(async () =>
    {
        var api = new FakeLarpEventApi();
        api.AddEvent("Fort", true, FirstDay);
        DayEditorWindow? shown = null;
        var dialogs = new FakeLarpEventDialogs
        {
            OnDayEditor = editor =>
            {
                shown = new DayEditorWindow(editor);
                shown.Show();
                Dispatcher.UIThread.RunJobs();
                return Task.CompletedTask;
            }
        };
        var screen = new LarpEventScreenViewModel(api, new JsonLocalizer(), dialogs, "http://localhost/api", "en", FakeLarpEventApi.EventsHref);
        await screen.LoadAsync();
        screen.SelectedEvent = screen.Events[0];
        screen.SelectedDay = screen.Days[1];

        await screen.EditDayCommand.ExecuteAsync();

        Assert.Equal(new DateTime(2026, 5, 1), shown!.GetVisualDescendants().OfType<CalendarDatePicker>().Single().SelectedDate);
        shown?.Close();
    });

    [Fact]
    public Task EventWindowShowsTheEventBeingEdited() => AvaloniaUi.Run(async () =>
    {
        var api = new FakeLarpEventApi();
        api.AddEvent("Fort", true, FirstDay);
        LarpEventEditorWindow? shown = null;
        var dialogs = new FakeLarpEventDialogs
        {
            OnEventEditor = editor =>
            {
                shown = new LarpEventEditorWindow(editor);
                shown.Show();
                Dispatcher.UIThread.RunJobs();
                return Task.CompletedTask;
            }
        };
        var screen = new LarpEventScreenViewModel(api, new JsonLocalizer(), dialogs, "http://localhost/api", "en", FakeLarpEventApi.EventsHref);
        await screen.LoadAsync();
        screen.SelectedEvent = screen.Events[0];

        await screen.EditEventCommand.ExecuteAsync();

        Assert.Contains(shown!.GetVisualDescendants().OfType<TextBox>(), box => box.Text == "Fort");
        shown?.Close();
    });

    [Fact]
    public Task ExperienceWindowSummarizesTheParticipant() => AvaloniaUi.Run(async () =>
    {
        var api = new FakeLarpEventApi();
        var fort = api.AddEvent("Fort", true, FirstDay);
        fort.Registrations[FakeLarpEventApi.KiraId] = [fort.Days[0].Id];
        ExperienceWindow? shown = null;
        var dialogs = new FakeLarpEventDialogs
        {
            OnExperience = editor =>
            {
                shown = new ExperienceWindow(editor);
                shown.Show();
                Dispatcher.UIThread.RunJobs();
                return Task.CompletedTask;
            }
        };
        var screen = new LarpEventScreenViewModel(api, new JsonLocalizer(), dialogs, "http://localhost/api", "en", FakeLarpEventApi.EventsHref);
        await screen.LoadAsync();
        screen.SelectedEvent = screen.Events[0];
        screen.SelectedParticipant = screen.Participants[0];

        await screen.ChangeExperienceCommand.ExecuteAsync();

        Assert.Contains(shown!.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == "Kira has 10 XP; 0 given at this event.");
        shown?.Close();
    });

    [Fact]
    public Task CloseWindowShowsABaseAndADayCheckboxPerCharacter() => AvaloniaUi.Run(async () =>
    {
        var api = new FakeLarpEventApi();
        var fort = api.AddEvent("Fort", true, FirstDay);
        fort.BaseExperience = 5;
        fort.Days[0] = fort.Days[0] with { Experience = 2 };
        fort.Registrations[FakeLarpEventApi.KiraId] = [fort.Days[0].Id];
        CloseEventWindow? shown = null;
        var dialogs = new FakeLarpEventDialogs
        {
            OnCloseEvent = editor =>
            {
                shown = new CloseEventWindow(editor);
                shown.Show();
                Dispatcher.UIThread.RunJobs();
                return Task.CompletedTask;
            }
        };
        var screen = new LarpEventScreenViewModel(api, new JsonLocalizer(), dialogs, "http://localhost/api", "en", FakeLarpEventApi.EventsHref);
        await screen.LoadAsync();
        screen.SelectedEvent = screen.Events[0];

        await screen.CloseEventCommand.ExecuteAsync();

        Assert.Equal(2, shown!.GetVisualDescendants().OfType<CheckBox>().Count(box => box.IsEffectivelyVisible && box.IsChecked == true));
        shown?.Close();
    });

    private static MainWindowViewModel CreateViewModel(FakeLarpEventApi api, FakeLarpEventDialogs dialogs) =>
        new(api, new NullSettingsStore(), new JsonLocalizer(), new FakeCatalogDialogs(), new FakeNetworkDialogs(), new FakePlayerDialogs(),
            new FakeCardTemplateDialogs(), dialogs, new FakeMessageDialogs(), new ManagementSettings());

    private sealed class NullSettingsStore : IManagementSettingsStore
    {
        public ManagementSettings Load() => new();

        public void Save(ManagementSettings settings)
        {
        }
    }
}
