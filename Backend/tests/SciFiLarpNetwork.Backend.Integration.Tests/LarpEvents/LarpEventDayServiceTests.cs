using SciFiLarpNetwork.Backend.Application.LarpEvents;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;

namespace SciFiLarpNetwork.Backend.Tests.Integration.LarpEvents;

[Collection("Backend database integration")]
public sealed class LarpEventDayServiceTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private readonly LarpEventTestData _data = new(factory);

    private ILarpEventDayService Days => _data.Days;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task DaysAreListedInDateOrderWithFirstAndLastDates()
    {
        var created = await _data.CreateEventAsync();
        await Days.AddDayAsync(created.EventId, new DateOnly(2026, 5, 3), Token);
        var result = await Days.AddDayAsync(created.EventId, new DateOnly(2026, 5, 1), Token);

        Assert.Equal(
            new { First = (DateOnly?)new DateOnly(2026, 5, 1), Last = (DateOnly?)new DateOnly(2026, 5, 3), Dates = "2026-05-01,2026-05-03" },
            new
            {
                First = result.Value!.FirstDate,
                Last = result.Value.LastDate,
                Dates = string.Join(',', result.Value.Days.Select(day => day.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)))
            });
    }

    [Fact]
    public async Task SecondDayOnTheSameDateIsRejected()
    {
        var created = await _data.CreateEventAsync(dayCount: 1);

        var result = await Days.AddDayAsync(created.EventId, LarpEventTestData.FirstDay, Token);

        Assert.Equal(LarpEventOutcome.DuplicateDate, result.Outcome);
    }

    [Fact]
    public async Task ClosedEventGetsNoNewDays()
    {
        var closed = await _data.CloseAsync(await _data.CreateEventAsync());

        var result = await Days.AddDayAsync(closed.EventId, LarpEventTestData.FirstDay, Token);

        Assert.Equal(LarpEventOutcome.EventClosed, result.Outcome);
    }

    [Fact]
    public async Task UnknownEventGetsNoDays()
    {
        var result = await Days.AddDayAsync(Guid.NewGuid(), LarpEventTestData.FirstDay, Token);

        Assert.Equal(LarpEventOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task RescheduledDayKeepsItsRegistrations()
    {
        var created = await _data.CreateEventAsync(dayCount: 1);
        var character = await _data.CreateCharacterAsync();
        var dayId = created.Days[0].DayId;
        await _data.Participants.RegisterAsync(created.EventId, character.CharacterId, [dayId], Token);

        var result = await Days.RescheduleDayAsync(created.EventId, dayId, new DateOnly(2026, 7, 1), Token);

        Assert.Equal([new LarpEventDaySummary(dayId, new DateOnly(2026, 7, 1), 1)], result.Value!.Days);
    }

    [Fact]
    public async Task DayCannotMoveOntoAnotherDaysDate()
    {
        var created = await _data.CreateEventAsync(dayCount: 2);

        var result = await Days.RescheduleDayAsync(created.EventId, created.Days[1].DayId, LarpEventTestData.FirstDay, Token);

        Assert.Equal(LarpEventOutcome.DuplicateDate, result.Outcome);
    }

    [Fact]
    public async Task UnknownDayCannotBeRescheduled()
    {
        var created = await _data.CreateEventAsync(dayCount: 1);

        var result = await Days.RescheduleDayAsync(created.EventId, Guid.NewGuid(), new DateOnly(2026, 7, 1), Token);

        Assert.Equal(LarpEventOutcome.UnknownDay, result.Outcome);
    }

    [Fact]
    public async Task EmptyDayIsRemoved()
    {
        var created = await _data.CreateEventAsync(dayCount: 2);

        var result = await Days.RemoveDayAsync(created.EventId, created.Days[0].DayId, Token);

        Assert.Equal([created.Days[1].DayId], result.Value!.Days.Select(day => day.DayId));
    }

    [Fact]
    public async Task DayWithParticipantsIsNotRemoved()
    {
        var created = await _data.CreateEventAsync(dayCount: 1);
        var character = await _data.CreateCharacterAsync();
        await _data.Participants.RegisterAsync(created.EventId, character.CharacterId, [created.Days[0].DayId], Token);

        var result = await Days.RemoveDayAsync(created.EventId, created.Days[0].DayId, Token);

        Assert.Equal(LarpEventOutcome.DayHasParticipants, result.Outcome);
    }
}
