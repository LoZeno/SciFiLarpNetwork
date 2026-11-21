using System.Net;
using System.Text.Json;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.ViewModels.LarpEvents;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.LarpEvents;

public sealed class ClosingGridTests
{
    private static readonly DateOnly FirstDay = new(2026, 5, 1);

    [Fact]
    public async Task GridHasABaseColumnAndOneColumnPerDay()
    {
        var api = EventWithExperience(baseExperience: 5, [2, null]);

        var grid = await OpenGridAsync(api);

        Assert.Equal(
            new { Base = "Base (5 XP)", Days = "2026-05-01 (2 XP)|2026-05-02 (—)" },
            new { Base = grid.BaseHeader, Days = string.Join('|', grid.DayHeaders) });
    }

    [Fact]
    public async Task EventWithoutBaseExperienceHasNoBaseColumn()
    {
        var api = EventWithExperience(baseExperience: null, [2]);

        var grid = await OpenGridAsync(api);

        Assert.False(grid.HasBaseColumn);
    }

    [Fact]
    public async Task ActiveCharacterStartsTicked()
    {
        var api = EventWithExperience(baseExperience: 5, [2]);

        var grid = await OpenGridAsync(api);

        Assert.Equal([true, true], Ticks(grid.Rows[0]));
    }

    [Fact]
    public async Task RetiredCharacterStartsUnticked()
    {
        var api = EventWithExperience(baseExperience: 5, [2]);
        api.InactiveCharacters.Add(FakeLarpEventApi.KiraId);

        var grid = await OpenGridAsync(api);

        Assert.Equal([false, false], Ticks(grid.Rows[0]));
    }

    [Fact]
    public async Task DayWithoutExperienceShowsADash()
    {
        var api = EventWithExperience(baseExperience: 5, [null]);

        var grid = await OpenGridAsync(api);

        Assert.Equal("—", grid.Rows[0].DayCells[0].Text);
    }

    [Fact]
    public async Task UnregisteredDayHasNoCheckbox()
    {
        var api = EventWithExperience(baseExperience: 5, [2, 3]);
        var fort = api.Events[0];
        fort.Registrations[FakeLarpEventApi.KiraId] = [fort.Days[0].Id];

        var grid = await OpenGridAsync(api);

        Assert.Equal([true, false], grid.Rows[0].DayCells.Select(cell => cell.IsCheckbox));
    }

    [Fact]
    public async Task ExperienceGivenByAnEarlierCloseShowsGiven()
    {
        var api = EventWithExperience(baseExperience: 5, [2]);
        var fort = api.Events[0];
        fort.BaseGiven.Add(FakeLarpEventApi.KiraId);
        fort.GivenDays[FakeLarpEventApi.KiraId] = [fort.Days[0].Id];

        var grid = await OpenGridAsync(api);

        Assert.Equal(["given", "given"], [grid.Rows[0].BaseCell!.Text, grid.Rows[0].DayCells[0].Text]);
    }

    [Fact]
    public async Task ToggleAllDaysClearsThemWhenAllAreTicked()
    {
        var api = EventWithExperience(baseExperience: 5, [2, 3]);
        var grid = await OpenGridAsync(api);

        grid.ToggleAllDaysCommand.Execute(null);

        Assert.Equal([true, false, false], Ticks(grid.Rows[0]));
    }

    [Fact]
    public async Task ToggleAllBaseTicksThemWhenAnyIsClear()
    {
        var api = EventWithExperience(baseExperience: 5, [2]);
        var grid = await OpenGridAsync(api);
        grid.Rows[0].BaseCell!.IsChecked = false;

        grid.ToggleAllBaseCommand.Execute(null);

        Assert.True(grid.Rows[0].BaseCell!.IsChecked);
    }

    [Fact]
    public async Task ConfirmedCloseSendsTheTickedCells()
    {
        var api = EventWithExperience(baseExperience: 5, [2, 3]);
        var fort = api.Events[0];
        var dialogs = new FakeLarpEventDialogs
        {
            OnCloseEvent = editor =>
            {
                editor.Rows[0].DayCells[1].IsChecked = false;
                return editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await OpenScreenAsync(api, dialogs);

        await screen.CloseEventCommand.ExecuteAsync();

        Assert.Equal(
            $$"""{"version":1,"awards":[{"characterId":"{{FakeLarpEventApi.KiraId:D}}","baseExperience":true,"dayIds":["{{fort.Days[0].Id:D}}"]}]}""",
            api.Submissions.Single(submission => submission.ActionName == "close-event").Body);
    }

    [Fact]
    public async Task CloseAsksForAFinalConfirmation()
    {
        var api = EventWithExperience(baseExperience: 5, [2]);
        var dialogs = new FakeLarpEventDialogs { OnCloseEvent = editor => editor.SaveCommand.ExecuteAsync() };
        var screen = await OpenScreenAsync(api, dialogs);

        await screen.CloseEventCommand.ExecuteAsync();

        Assert.Equal("Close the event \"Fort\" and give the selected experience? The selection is final.", dialogs.LastConfirmation?.Message);
    }

    [Fact]
    public async Task StaleCloseReloadsTheGridAndKeepsTheDialogOpen()
    {
        var api = EventWithExperience(baseExperience: 5, [2]);
        api.Failures["close-event"] = new ManagementApiException("errors.staleVersion", HttpStatusCode.Conflict);
        CloseEventViewModel? shown = null;
        var dialogs = new FakeLarpEventDialogs
        {
            OnCloseEvent = editor =>
            {
                shown = editor;
                api.Events[0].Registrations[FakeLarpEventApi.OdoId] = [api.Events[0].Days[0].Id];
                return editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await OpenScreenAsync(api, dialogs);

        await screen.CloseEventCommand.ExecuteAsync();

        Assert.Equal(new { Saved = false, Rows = 2 }, new { Saved = shown!.IsSaved, Rows = shown.Rows.Count });
    }

    [Fact]
    public async Task EventDialogSendsTheBaseExperience()
    {
        var api = new FakeLarpEventApi();
        var dialogs = new FakeLarpEventDialogs
        {
            OnEventEditor = editor =>
            {
                editor.Name = "Fort";
                editor.BaseExperience = "5";
                return editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = new LarpEventScreenViewModel(api, new JsonLocalizer(), dialogs, "http://localhost/api", "en", FakeLarpEventApi.EventsHref);
        await screen.LoadAsync();

        await screen.NewEventCommand.ExecuteAsync();

        Assert.Equal(5, JsonDocument.Parse(api.Submissions.Single().Body!).RootElement.GetProperty("baseExperience").GetInt32());
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("five")]
    public async Task InvalidDayExperienceIsNotSent(string experience)
    {
        var api = EventWithExperience(baseExperience: null, [2]);
        var dialogs = new FakeLarpEventDialogs
        {
            OnDayEditor = editor =>
            {
                editor.Experience = experience;
                return editor.SaveCommand.ExecuteAsync();
            }
        };
        var screen = await OpenScreenAsync(api, dialogs);

        await screen.AddDayCommand.ExecuteAsync();

        Assert.DoesNotContain(api.Submissions, submission => submission.ActionName == "add-day");
    }

    [Fact]
    public async Task DayListShowsTheDayExperience()
    {
        var api = EventWithExperience(baseExperience: null, [2]);

        var screen = await OpenScreenAsync(api, new FakeLarpEventDialogs());

        Assert.Equal("2026-05-01 (1 characters, 2 XP)", screen.Days[1].DisplayName);
    }

    private static FakeLarpEventApi EventWithExperience(int? baseExperience, int?[] dayExperience)
    {
        var api = new FakeLarpEventApi();
        var fort = api.AddEvent("Fort", true, [.. dayExperience.Select((_, index) => FirstDay.AddDays(index))]);
        fort.BaseExperience = baseExperience;
        for (var index = 0; index < dayExperience.Length; index++)
        {
            fort.Days[index] = fort.Days[index] with { Experience = dayExperience[index] };
        }

        fort.Registrations[FakeLarpEventApi.KiraId] = [.. fort.Days.Select(day => day.Id)];
        return api;
    }

    private static async Task<LarpEventScreenViewModel> OpenScreenAsync(FakeLarpEventApi api, FakeLarpEventDialogs dialogs)
    {
        var screen = new LarpEventScreenViewModel(api, new JsonLocalizer(), dialogs, "http://localhost/api", "en", FakeLarpEventApi.EventsHref);
        await screen.LoadAsync();
        screen.SelectedEvent = screen.Events[0];
        return screen;
    }

    private static async Task<CloseEventViewModel> OpenGridAsync(FakeLarpEventApi api)
    {
        CloseEventViewModel? shown = null;
        var dialogs = new FakeLarpEventDialogs
        {
            OnCloseEvent = editor =>
            {
                shown = editor;
                return Task.CompletedTask;
            }
        };
        var screen = await OpenScreenAsync(api, dialogs);
        await screen.CloseEventCommand.ExecuteAsync();
        return shown!;
    }

    private static bool[] Ticks(ClosingRowViewModel row) =>
        [.. new[] { row.BaseCell }.Concat(row.DayCells).OfType<ClosingCellViewModel>().Where(cell => cell.IsCheckbox).Select(cell => cell.IsChecked)];
}
