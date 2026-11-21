using SciFiLarpNetwork.Backend.Application.LarpEvents;
using SciFiLarpNetwork.Backend.Domain.LarpEvents;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;

namespace SciFiLarpNetwork.Backend.Tests.Integration.LarpEvents;

[Collection("Backend database integration")]
public sealed class LarpEventServiceTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private readonly LarpEventTestData _data = new(factory);

    private ILarpEventService Events => _data.Events;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CreatedEventIsOpenWithoutDays()
    {
        var result = await Events.CreateEventAsync(new LarpEventDefinition("  Fort weekend ", " Bring tents "), Token);

        Assert.Equivalent(
            new LarpEventSummary(result.Value!.EventId, 1, "Fort weekend", "Bring tents", LarpEventStatus.Open, false, null, null, [], 0, 1),
            result.Value,
            strict: true);
    }

    [Fact]
    public async Task EventsGetRunningNumbersInCreationOrder()
    {
        var numbers = new List<int>();
        foreach (var name in new[] { "One", "Two", "Three" })
        {
            numbers.Add((await _data.CreateEventAsync(name)).Number);
        }

        Assert.Equal([1, 2, 3], numbers);
    }

    [Fact]
    public async Task InvalidEventIsRejectedAndNotStored()
    {
        var result = await Events.CreateEventAsync(new LarpEventDefinition(" ", null), Token);

        var stored = await Events.GetEventsAsync(new LarpEventQuery(true, 1), Token);
        Assert.Equal(new { Outcome = LarpEventOutcome.InvalidName, Stored = 0L }, new { result.Outcome, Stored = stored.TotalCount });
    }

    [Fact]
    public async Task EditChangesTheDetails()
    {
        var created = await _data.CreateEventAsync();

        var result = await Events.UpdateEventAsync(created.EventId, created.Version, new LarpEventDefinition("Fort", "Tents"), Token);

        Assert.Equivalent(created with { Name = "Fort", Description = "Tents", Version = 2 }, result.Value, strict: true);
    }

    [Fact]
    public async Task StaleEditIsRejected()
    {
        var created = await _data.CreateEventAsync();
        await Events.UpdateEventAsync(created.EventId, created.Version, new LarpEventDefinition("Fort", null), Token);

        var result = await Events.UpdateEventAsync(created.EventId, created.Version, new LarpEventDefinition("Keep", null), Token);

        Assert.Equal(LarpEventOutcome.StaleVersion, result.Outcome);
    }

    [Fact]
    public async Task EditStartedBeforeADayWasAddedIsStale()
    {
        var created = await _data.CreateEventAsync();
        await _data.Days.AddDayAsync(created.EventId, LarpEventTestData.FirstDay, Token);

        var result = await Events.UpdateEventAsync(created.EventId, created.Version, new LarpEventDefinition("Fort", null), Token);

        Assert.Equal(LarpEventOutcome.StaleVersion, result.Outcome);
    }

    [Fact]
    public async Task ClosedEventCannotBeEdited()
    {
        var closed = await _data.CloseAsync(await _data.CreateEventAsync());

        var result = await Events.UpdateEventAsync(closed.EventId, closed.Version, new LarpEventDefinition("Fort", null), Token);

        Assert.Equal(LarpEventOutcome.EventClosed, result.Outcome);
    }

    [Fact]
    public async Task ReopenedEventIsOpen()
    {
        var closed = await _data.CloseAsync(await _data.CreateEventAsync());

        var result = await Events.ReopenEventAsync(closed.EventId, Token);

        Assert.Equal(LarpEventStatus.Open, result.Value!.Status);
    }

    [Fact]
    public async Task OpenEventCannotBeArchived()
    {
        var created = await _data.CreateEventAsync();

        var result = await Events.ArchiveEventAsync(created.EventId, Token);

        Assert.Equal(LarpEventOutcome.EventNotClosed, result.Outcome);
    }

    [Fact]
    public async Task ArchivedEventCannotBeReopened()
    {
        var closed = await _data.CloseAsync(await _data.CreateEventAsync());
        await Events.ArchiveEventAsync(closed.EventId, Token);

        var result = await Events.ReopenEventAsync(closed.EventId, Token);

        Assert.Equal(LarpEventOutcome.Archived, result.Outcome);
    }

    [Fact]
    public async Task UnarchivedEventIsListedAgain()
    {
        var closed = await _data.CloseAsync(await _data.CreateEventAsync());
        await Events.ArchiveEventAsync(closed.EventId, Token);
        await Events.UnarchiveEventAsync(closed.EventId, Token);

        var page = await Events.GetEventsAsync(new LarpEventQuery(false, 1), Token);

        Assert.Equal([closed.EventId], page.Items.Select(larpEvent => larpEvent.EventId));
    }

    [Fact]
    public async Task ArchivedEventsAreHiddenByDefault()
    {
        var closed = await _data.CloseAsync(await _data.CreateEventAsync("Old"));
        await Events.ArchiveEventAsync(closed.EventId, Token);
        await _data.CreateEventAsync("New");

        var page = await Events.GetEventsAsync(new LarpEventQuery(false, 1), Token);

        Assert.Equal(["New"], page.Items.Select(larpEvent => larpEvent.Name));
    }

    [Fact]
    public async Task ListShowsEventsWithoutDaysFirstThenNewestFirstDay()
    {
        var early = await _data.CreateEventAsync("Early");
        var late = await _data.CreateEventAsync("Late");
        await _data.CreateEventAsync("Unscheduled");
        await _data.Days.AddDayAsync(early.EventId, new DateOnly(2026, 1, 10), Token);
        await _data.Days.AddDayAsync(late.EventId, new DateOnly(2026, 6, 10), Token);
        await _data.CreateEventAsync("Also unscheduled");

        var page = await Events.GetEventsAsync(new LarpEventQuery(false, 1), Token);

        Assert.Equal(["Also unscheduled", "Unscheduled", "Late", "Early"], page.Items.Select(larpEvent => larpEvent.Name));
    }

    [Fact]
    public async Task ListIsPagedBy50()
    {
        for (var index = 0; index < 51; index++)
        {
            await _data.CreateEventAsync($"Event {index}");
        }

        var page = await Events.GetEventsAsync(new LarpEventQuery(false, 2), Token);

        Assert.Equal(new { Count = 1, PageCount = 2, Total = 51L }, new { page.Items.Count, page.PageCount, Total = page.TotalCount });
    }
}
