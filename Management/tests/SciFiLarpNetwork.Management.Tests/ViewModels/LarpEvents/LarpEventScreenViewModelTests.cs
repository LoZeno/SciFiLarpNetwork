using System.Net;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels.LarpEvents;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.LarpEvents;

public sealed class LarpEventScreenViewModelTests
{
    private static readonly DateOnly FirstDay = new(2026, 5, 1);

    [Fact]
    public async Task EventsAreListedWithTheirDatesAndStatus()
    {
        var api = new FakeLarpEventApi();
        api.AddEvent("Fort", true, FirstDay, FirstDay.AddDays(1));

        var screen = await OpenAsync(api, new FakeLarpEventDialogs());

        Assert.Equal(["#1 Fort · 2026-05-01 – 2026-05-02 (Open)"], screen.Events.Select(item => item.DisplayName));
    }

    [Fact]
    public async Task EmptyListShowsAPlaceholder()
    {
        var screen = await OpenAsync(new FakeLarpEventApi(), new FakeLarpEventDialogs());

        Assert.Equal("No events yet.", screen.EventsPlaceholder);
    }

    [Fact]
    public async Task SelectingAnEventListsAllDaysThenEachDay()
    {
        var api = new FakeLarpEventApi();
        api.AddEvent("Fort", true, FirstDay, FirstDay.AddDays(1));
        var screen = await OpenAsync(api, new FakeLarpEventDialogs());

        screen.SelectedEvent = screen.Events[0];

        Assert.Equal(
            ["All days (0 characters)", "2026-05-01 (0 characters)", "2026-05-02 (0 characters)"],
            screen.Days.Select(day => day.DisplayName));
    }

    [Fact]
    public async Task EventWithoutDaysShowsAPlaceholder()
    {
        var api = new FakeLarpEventApi();
        api.AddEvent("Fort");
        var screen = await OpenAsync(api, new FakeLarpEventDialogs());

        screen.SelectedEvent = screen.Events[0];

        Assert.Equal("This event has no days yet.", screen.DaysPlaceholder);
    }

    [Fact]
    public async Task SelectingADayListsOnlyItsParticipants()
    {
        var api = new FakeLarpEventApi();
        var fort = api.AddEvent("Fort", true, FirstDay, FirstDay.AddDays(1));
        fort.Registrations[FakeLarpEventApi.KiraId] = [fort.Days[0].Id];
        fort.Registrations[FakeLarpEventApi.OdoId] = [fort.Days[1].Id];
        var screen = await OpenWithEventAsync(api, new FakeLarpEventDialogs());

        screen.SelectedDay = screen.Days[2];

        Assert.Equal(["Odo"], screen.Participants.Select(participant => participant.CharacterName));
    }

    [Fact]
    public async Task AllDaysListsEveryParticipant()
    {
        var api = new FakeLarpEventApi();
        var fort = api.AddEvent("Fort", true, FirstDay, FirstDay.AddDays(1));
        fort.Registrations[FakeLarpEventApi.KiraId] = [fort.Days[0].Id];
        fort.Registrations[FakeLarpEventApi.OdoId] = [fort.Days[1].Id];

        var screen = await OpenWithEventAsync(api, new FakeLarpEventDialogs());

        Assert.Equal(["Kira", "Odo"], screen.Participants.Select(participant => participant.CharacterName));
    }

    [Fact]
    public async Task ParticipantShowsDaysAndExperienceGiven()
    {
        var api = new FakeLarpEventApi();
        var fort = api.AddEvent("Fort", true, FirstDay);
        fort.Registrations[FakeLarpEventApi.KiraId] = [fort.Days[0].Id];
        fort.Experience[FakeLarpEventApi.KiraId] = 3;

        var screen = await OpenWithEventAsync(api, new FakeLarpEventDialogs());

        Assert.Equal("#1 Kira — Ann · 2026-05-01 · XP +3", screen.Participants[0].DisplayName);
    }

    [Fact]
    public async Task ClosedEventExplainsWhyItCannotChange()
    {
        var api = new FakeLarpEventApi();
        api.AddEvent("Fort", isOpen: false, FirstDay);

        var screen = await OpenWithEventAsync(api, new FakeLarpEventDialogs());

        Assert.Equal(
            new { Closed = true, Explanation = "This event is closed, so its details, days, registrations, and experience cannot change. Reopen it to make changes." },
            new { Closed = screen.IsSelectedEventClosed, Explanation = screen.ClosedExplanation });
    }

    [Fact]
    public async Task ClosedEventOffersNoChanges()
    {
        var api = new FakeLarpEventApi();
        api.AddEvent("Fort", isOpen: false, FirstDay);

        var screen = await OpenWithEventAsync(api, new FakeLarpEventDialogs());

        Assert.Equal(
            [false, false, false, false, true],
            [
                screen.AddDayCommand.CanExecute(null), screen.RegisterCommand.CanExecute(null), screen.EditEventCommand.CanExecute(null),
                screen.CloseEventCommand.CanExecute(null), screen.ReopenEventCommand.CanExecute(null)
            ]);
    }

    [Fact]
    public async Task ReaderGetsNoChangeCommands()
    {
        var api = new FakeLarpEventApi { CanWrite = false };
        api.AddEvent("Fort", true, FirstDay);

        var screen = await OpenWithEventAsync(api, new FakeLarpEventDialogs());

        Assert.Equal(
            [false, false, false],
            [screen.CanWrite, screen.NewEventCommand.CanExecute(null), screen.AddDayCommand.CanExecute(null)]);
    }

    [Fact]
    public async Task NewEventIsCreatedAndSelected()
    {
        var api = new FakeLarpEventApi();
        var dialogs = new FakeLarpEventDialogs
        {
            OnEventEditor = async editor =>
            {
                editor.Name = "Fort";
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await OpenAsync(api, dialogs);

        await screen.NewEventCommand.ExecuteAsync();

        Assert.Equal("Fort", screen.SelectedEvent?.Name);
    }

    [Fact]
    public async Task FailedSaveKeepsTheDialogOpenWithTheError()
    {
        var api = new FakeLarpEventApi();
        api.Failures["create-event"] = new ManagementApiException("errors.invalidName", HttpStatusCode.BadRequest);
        LarpEventEditorViewModel? shown = null;
        var dialogs = new FakeLarpEventDialogs
        {
            OnEventEditor = async editor =>
            {
                shown = editor;
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await OpenAsync(api, dialogs);

        await screen.NewEventCommand.ExecuteAsync();

        Assert.Equal(new { Saved = false, HasError = true }, new { Saved = shown!.IsSaved, shown.HasError });
    }

    [Fact]
    public async Task ClosingAnEventAsksFirst()
    {
        var api = new FakeLarpEventApi();
        api.AddEvent("Fort", true, FirstDay);
        var dialogs = new FakeLarpEventDialogs { ConfirmResult = false };
        var screen = await OpenWithEventAsync(api, dialogs);

        await screen.CloseEventCommand.ExecuteAsync();

        Assert.Equal(new { Submitted = false, Asked = true },
            new { Submitted = api.Submissions.Any(submission => submission.ActionName == "close-event"), Asked = dialogs.LastConfirmation is not null });
    }

    [Fact]
    public async Task ConfirmedCloseRefreshesTheEvent()
    {
        var api = new FakeLarpEventApi();
        api.AddEvent("Fort", true, FirstDay);
        var screen = await OpenWithEventAsync(api, new FakeLarpEventDialogs());

        await screen.CloseEventCommand.ExecuteAsync();

        Assert.True(screen.IsSelectedEventClosed);
    }

    [Fact]
    public async Task AddedDayAppearsInTheList()
    {
        var api = new FakeLarpEventApi();
        api.AddEvent("Fort", true, FirstDay);
        var dialogs = new FakeLarpEventDialogs { OnDayEditor = editor => editor.SaveCommand.ExecuteAsync() };
        var screen = await OpenWithEventAsync(api, dialogs);

        await screen.AddDayCommand.ExecuteAsync();

        Assert.Equal("2026-05-02 (0 characters)", screen.Days[^1].DisplayName);
    }

    [Fact]
    public async Task DayDialogWithoutADateIsNotSent()
    {
        var api = new FakeLarpEventApi();
        api.AddEvent("Fort", true, FirstDay);
        DayEditorViewModel? shown = null;
        var dialogs = new FakeLarpEventDialogs
        {
            OnDayEditor = async editor =>
            {
                shown = editor;
                editor.Date = null;
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await OpenWithEventAsync(api, dialogs);

        await screen.AddDayCommand.ExecuteAsync();

        Assert.Equal(new { Sent = false, Error = (string?)"Choose a date." },
            new { Sent = api.Submissions.Any(submission => submission.ActionName == "add-day"), Error = shown!.ErrorMessage });
    }

    [Fact]
    public async Task RescheduleNeedsASingleDay()
    {
        var api = new FakeLarpEventApi();
        api.AddEvent("Fort", true, FirstDay);

        var screen = await OpenWithEventAsync(api, new FakeLarpEventDialogs());

        Assert.False(screen.RescheduleDayCommand.CanExecute(null));
    }

    [Fact]
    public async Task RegistrationSendsOnlyTheChosenCharacterAndNewDays()
    {
        var api = new FakeLarpEventApi();
        var fort = api.AddEvent("Fort", true, FirstDay, FirstDay.AddDays(1));
        fort.Registrations[FakeLarpEventApi.KiraId] = [fort.Days[0].Id];
        var dialogs = new FakeLarpEventDialogs
        {
            OnRegistration = async editor =>
            {
                editor.SearchText = "kira";
                await editor.SearchCommand.ExecuteAsync();
                editor.SelectAllDaysCommand.Execute(null);
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await OpenWithEventAsync(api, dialogs);

        await screen.RegisterCommand.ExecuteAsync();

        Assert.Equal(
            $$"""{"characterId":"{{FakeLarpEventApi.KiraId:D}}","dayIds":["{{fort.Days[1].Id:D}}"]}""",
            api.Submissions.Single(submission => submission.ActionName == "register-participant").Body);
    }

    [Fact]
    public async Task RegistrationExplainsDaysAlreadyRegistered()
    {
        var api = new FakeLarpEventApi();
        var fort = api.AddEvent("Fort", true, FirstDay, FirstDay.AddDays(1));
        fort.Registrations[FakeLarpEventApi.KiraId] = [fort.Days[0].Id];
        RegistrationEditorViewModel? shown = null;
        var dialogs = new FakeLarpEventDialogs
        {
            OnRegistration = async editor =>
            {
                shown = editor;
                editor.SearchText = "kira";
                await editor.SearchCommand.ExecuteAsync();
            }
        };
        var screen = await OpenWithEventAsync(api, dialogs);

        await screen.RegisterCommand.ExecuteAsync();

        Assert.Equal("Already registered on: 2026-05-01. Only the other days can be added.", shown!.AlreadyRegisteredText);
    }

    [Fact]
    public async Task RegistrationForEveryRegisteredDayIsNotSent()
    {
        var api = new FakeLarpEventApi();
        var fort = api.AddEvent("Fort", true, FirstDay);
        fort.Registrations[FakeLarpEventApi.KiraId] = [fort.Days[0].Id];
        RegistrationEditorViewModel? shown = null;
        var dialogs = new FakeLarpEventDialogs
        {
            OnRegistration = async editor =>
            {
                shown = editor;
                editor.SearchText = "kira";
                await editor.SearchCommand.ExecuteAsync();
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await OpenWithEventAsync(api, dialogs);

        await screen.RegisterCommand.ExecuteAsync();

        Assert.Equal("The character is already registered for every selected day.", shown!.ErrorMessage);
    }

    [Fact]
    public async Task UnregisteringFromADaySendsOnlyThatDay()
    {
        var api = new FakeLarpEventApi();
        var fort = api.AddEvent("Fort", true, FirstDay, FirstDay.AddDays(1));
        fort.Registrations[FakeLarpEventApi.KiraId] = [fort.Days[0].Id, fort.Days[1].Id];
        var screen = await OpenWithEventAsync(api, new FakeLarpEventDialogs());
        screen.SelectedDay = screen.Days[1];
        screen.SelectedParticipant = screen.Participants[0];

        await screen.UnregisterCommand.ExecuteAsync();

        Assert.Equal(
            $$"""{"characterId":"{{FakeLarpEventApi.KiraId:D}}","dayIds":["{{fort.Days[0].Id:D}}"]}""",
            api.Submissions.Single(submission => submission.ActionName == "unregister-participant").Body);
    }

    [Fact]
    public async Task ExperienceChangeIsSentAndShown()
    {
        var api = new FakeLarpEventApi();
        var fort = api.AddEvent("Fort", true, FirstDay);
        fort.Registrations[FakeLarpEventApi.KiraId] = [fort.Days[0].Id];
        var dialogs = new FakeLarpEventDialogs
        {
            OnExperience = async editor =>
            {
                editor.Amount = "-2";
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await OpenWithEventAsync(api, dialogs);
        screen.SelectedParticipant = screen.Participants[0];

        await screen.ChangeExperienceCommand.ExecuteAsync();

        Assert.Equal("#1 Kira — Ann · 2026-05-01 · XP -2", screen.Participants[0].DisplayName);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("two")]
    [InlineData("")]
    public async Task ExperienceChangeMustBeANonZeroWholeNumber(string amount)
    {
        var api = new FakeLarpEventApi();
        var fort = api.AddEvent("Fort", true, FirstDay);
        fort.Registrations[FakeLarpEventApi.KiraId] = [fort.Days[0].Id];
        var dialogs = new FakeLarpEventDialogs
        {
            OnExperience = async editor =>
            {
                editor.Amount = amount;
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await OpenWithEventAsync(api, dialogs);
        screen.SelectedParticipant = screen.Participants[0];

        await screen.ChangeExperienceCommand.ExecuteAsync();

        Assert.DoesNotContain(api.Submissions, submission => submission.ActionName == "change-experience");
    }

    [Fact]
    public async Task RejectedExperienceChangeShowsTheReason()
    {
        var api = new FakeLarpEventApi();
        var fort = api.AddEvent("Fort", true, FirstDay);
        fort.Registrations[FakeLarpEventApi.KiraId] = [fort.Days[0].Id];
        api.Failures["change-experience"] = new ManagementApiException("errors.insufficientFreePoints", HttpStatusCode.Conflict);
        ExperienceEditorViewModel? shown = null;
        var dialogs = new FakeLarpEventDialogs
        {
            OnExperience = async editor =>
            {
                shown = editor;
                editor.Amount = "-5";
                await editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await OpenWithEventAsync(api, dialogs);
        screen.SelectedParticipant = screen.Participants[0];

        await screen.ChangeExperienceCommand.ExecuteAsync();

        Assert.Equal(new JsonLocalizer().TranslateApiError("errors.insufficientFreePoints", "en"), shown!.ErrorMessage);
    }

    [Fact]
    public async Task PrintingSendsTheSelectedDay()
    {
        var api = new FakeLarpEventApi();
        var fort = api.AddEvent("Fort", true, FirstDay);
        fort.Registrations[FakeLarpEventApi.KiraId] = [fort.Days[0].Id];
        var screen = await OpenWithEventAsync(api, new FakeLarpEventDialogs());
        screen.SelectedDay = screen.Days[1];

        await screen.PreviewCardsCommand.ExecuteAsync();

        Assert.Equal($$"""{"dayId":"{{fort.Days[0].Id:D}}"}""", api.Submissions.Single(submission => submission.ActionName == "print-event-cards").Body);
    }

    [Fact]
    public async Task ExportedCardsAreSaved()
    {
        var api = new FakeLarpEventApi();
        var fort = api.AddEvent("Fort", true, FirstDay);
        fort.Registrations[FakeLarpEventApi.KiraId] = [fort.Days[0].Id];
        var dialogs = new FakeLarpEventDialogs();
        var screen = await OpenWithEventAsync(api, dialogs);

        await screen.ExportCardsCommand.ExecuteAsync();

        Assert.Equal(FakeLarpEventApi.CardsPdf, dialogs.LastExportedPdf);
    }

    [Fact]
    public async Task NothingToPrintWithoutParticipants()
    {
        var api = new FakeLarpEventApi();
        api.AddEvent("Fort", true, FirstDay);

        var screen = await OpenWithEventAsync(api, new FakeLarpEventDialogs());

        Assert.False(screen.PreviewCardsCommand.CanExecute(null));
    }

    [Fact]
    public async Task ItalianLabelsFollowTheLocale()
    {
        var screen = await OpenAsync(new FakeLarpEventApi(), new FakeLarpEventDialogs(), "it");

        Assert.Equal("Ancora nessun evento.", screen.EventsPlaceholder);
    }

    private static async Task<LarpEventScreenViewModel> OpenAsync(FakeLarpEventApi api, FakeLarpEventDialogs dialogs, string locale = "en")
    {
        var screen = new LarpEventScreenViewModel(api, new JsonLocalizer(), dialogs, "http://localhost/api", locale,
            FakeLarpEventApi.EventsHref);
        await screen.LoadAsync();
        return screen;
    }

    private static async Task<LarpEventScreenViewModel> OpenWithEventAsync(FakeLarpEventApi api, FakeLarpEventDialogs dialogs)
    {
        var screen = await OpenAsync(api, dialogs);
        screen.SelectedEvent = screen.Events[0];
        return screen;
    }
}
