using SciFiLarpNetwork.Backend.Application.LarpEvents;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.LarpEvents;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;
using SciFiLarpNetwork.Backend.Tests.Integration.Players;

namespace SciFiLarpNetwork.Backend.Tests.Integration.LarpEvents;

[Collection("Backend database integration")]
public sealed class LarpEventClosingTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private readonly LarpEventTestData _data = new(factory);

    private ILarpEventService Events => _data.Events;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task EventKeepsItsBaseExperience()
    {
        var result = await Events.CreateEventAsync(new LarpEventDefinition("Fort", null, 5), Token);

        Assert.Equal(5, result.Value!.BaseExperience);
    }

    [Fact]
    public async Task NegativeBaseExperienceIsRejected()
    {
        var result = await Events.CreateEventAsync(new LarpEventDefinition("Fort", null, -1), Token);

        Assert.Equal(LarpEventOutcome.InvalidExperienceValue, result.Outcome);
    }

    [Fact]
    public async Task DayKeepsItsExperience()
    {
        var created = await _data.CreateEventAsync();

        var result = await _data.Days.AddDayAsync(created.EventId, LarpEventTestData.FirstDay, 3, Token);

        Assert.Equal([3], result.Value!.Days.Select(day => day.Experience));
    }

    [Fact]
    public async Task NegativeDayExperienceIsRejected()
    {
        var created = await _data.CreateEventAsync();

        var result = await _data.Days.AddDayAsync(created.EventId, LarpEventTestData.FirstDay, -3, Token);

        Assert.Equal(LarpEventOutcome.InvalidExperienceValue, result.Outcome);
    }

    [Fact]
    public async Task EditingADayChangesItsExperience()
    {
        var created = await _data.CreateEventAsync(dayCount: 1);

        var result = await _data.Days.UpdateDayAsync(created.EventId, created.Days[0].DayId, LarpEventTestData.FirstDay, 7, Token);

        Assert.Equal([7], result.Value!.Days.Select(day => day.Experience));
    }

    [Fact]
    public async Task ClosingGivesTheSelectedBaseAndDayExperience()
    {
        var (larpEvent, character) = await ScheduledEventAsync(baseExperience: 5, dayExperience: [2, 3]);

        await CloseAsync(larpEvent, new ClosingAward(character.CharacterId, true, LarpEventTestData.AllDays(larpEvent)));

        Assert.Equal(20, await ExperienceOfAsync(character));
    }

    [Fact]
    public async Task ClosingGivesOnlyTheChosenDays()
    {
        var (larpEvent, character) = await ScheduledEventAsync(baseExperience: 5, dayExperience: [2, 3]);

        await CloseAsync(larpEvent, new ClosingAward(character.CharacterId, false, [larpEvent.Days[1].DayId]));

        Assert.Equal(13, await ExperienceOfAsync(character));
    }

    [Fact]
    public async Task ClosingWithNothingChosenGivesNothing()
    {
        var (larpEvent, character) = await ScheduledEventAsync(baseExperience: 5, dayExperience: [2, 3]);

        await CloseAsync(larpEvent);

        Assert.Equal(10, await ExperienceOfAsync(character));
    }

    [Fact]
    public async Task ClosingRecordsTheAwardOnTheEvent()
    {
        var (larpEvent, character) = await ScheduledEventAsync(baseExperience: 5, dayExperience: [2, 3]);
        await CloseAsync(larpEvent, new ClosingAward(character.CharacterId, true, [larpEvent.Days[0].DayId]));

        var participant = (await _data.Participants.GetParticipantsAsync(larpEvent.EventId, null, Token)).Value![0];

        Assert.Equivalent(
            new { BaseExperienceGiven = true, GivenDayIds = new[] { larpEvent.Days[0].DayId }, Amount = 7, IsClosingAward = true },
            new
            {
                participant.BaseExperienceGiven,
                participant.GivenDayIds,
                participant.ExperienceChanges[0].Amount,
                participant.ExperienceChanges[0].IsClosingAward
            },
            strict: true);
    }

    [Fact]
    public async Task ClosingAfterAReopenNeverGivesTheSameExperienceTwice()
    {
        var (larpEvent, character) = await ScheduledEventAsync(baseExperience: 5, dayExperience: [2, 3]);
        await CloseAsync(larpEvent, new ClosingAward(character.CharacterId, true, [larpEvent.Days[0].DayId]));
        await Events.ReopenEventAsync(larpEvent.EventId, Token);

        var result = await CloseAsync(larpEvent, new ClosingAward(character.CharacterId, true, [larpEvent.Days[0].DayId]));

        Assert.Equal(LarpEventOutcome.AwardNotAllowed, result.Outcome);
    }

    [Fact]
    public async Task ClosingAfterAReopenGivesTheRemainingDays()
    {
        var (larpEvent, character) = await ScheduledEventAsync(baseExperience: 5, dayExperience: [2, 3]);
        await CloseAsync(larpEvent, new ClosingAward(character.CharacterId, true, [larpEvent.Days[0].DayId]));
        await Events.ReopenEventAsync(larpEvent.EventId, Token);

        await CloseAsync(larpEvent, new ClosingAward(character.CharacterId, false, [larpEvent.Days[1].DayId]));

        Assert.Equal(20, await ExperienceOfAsync(character));
    }

    [Fact]
    public async Task DayWithoutExperienceCannotBeAwarded()
    {
        var (larpEvent, character) = await ScheduledEventAsync(baseExperience: 5, dayExperience: [null]);

        var result = await CloseAsync(larpEvent, new ClosingAward(character.CharacterId, false, LarpEventTestData.AllDays(larpEvent)));

        Assert.Equal(LarpEventOutcome.AwardNotAllowed, result.Outcome);
    }

    [Fact]
    public async Task BaseExperienceCannotBeAwardedWhenTheEventHasNone()
    {
        var (larpEvent, character) = await ScheduledEventAsync(baseExperience: null, dayExperience: [2]);

        var result = await CloseAsync(larpEvent, new ClosingAward(character.CharacterId, true, []));

        Assert.Equal(LarpEventOutcome.AwardNotAllowed, result.Outcome);
    }

    [Fact]
    public async Task UnregisteredDayCannotBeAwarded()
    {
        var larpEvent = await _data.CreateEventAsync();
        larpEvent = (await _data.Days.AddDayAsync(larpEvent.EventId, LarpEventTestData.FirstDay, 2, Token)).Value!;
        larpEvent = (await _data.Days.AddDayAsync(larpEvent.EventId, LarpEventTestData.FirstDay.AddDays(1), 3, Token)).Value!;
        var character = await _data.CreateCharacterAsync(experiencePoints: 10);
        await _data.Participants.RegisterAsync(larpEvent.EventId, character.CharacterId, [larpEvent.Days[0].DayId], Token);

        var result = await CloseAsync(larpEvent, new ClosingAward(character.CharacterId, false, [larpEvent.Days[1].DayId]));

        Assert.Equal(LarpEventOutcome.AwardNotAllowed, result.Outcome);
    }

    [Fact]
    public async Task RejectedCloseLeavesTheEventOpen()
    {
        var (larpEvent, character) = await ScheduledEventAsync(baseExperience: null, dayExperience: [2]);
        await CloseAsync(larpEvent, new ClosingAward(character.CharacterId, true, []));

        Assert.Equal(LarpEventStatus.Open, (await Events.GetEventAsync(larpEvent.EventId, Token))!.Status);
    }

    [Fact]
    public async Task RetiredCharacterCanStillGetExperience()
    {
        var (larpEvent, character) = await ScheduledEventAsync(baseExperience: 5, dayExperience: [2]);
        await _data.Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
            CharacterDefinitions.From(character) with { Status = Domain.Characters.CharacterStatus.Retired }, Token);

        await CloseAsync(larpEvent, new ClosingAward(character.CharacterId, true, LarpEventTestData.AllDays(larpEvent)));

        Assert.Equal(17, await ExperienceOfAsync(character));
    }

    [Fact]
    public async Task CloseFromAGridBuiltBeforeARegistrationIsStale()
    {
        var (larpEvent, character) = await ScheduledEventAsync(baseExperience: 5, dayExperience: [2]);
        var gridVersion = (await Events.GetEventAsync(larpEvent.EventId, Token))!.Version;
        var other = await _data.CreateCharacterAsync("Odo");
        await _data.Participants.RegisterAsync(larpEvent.EventId, other.CharacterId, LarpEventTestData.AllDays(larpEvent), Token);

        var result = await Events.CloseEventAsync(larpEvent.EventId, gridVersion,
            [new ClosingAward(character.CharacterId, true, [])], Token);

        Assert.Equal(LarpEventOutcome.StaleVersion, result.Outcome);
    }

    [Fact]
    public async Task AwardAboveTheExperienceMaximumIsRejected()
    {
        var (larpEvent, character) = await ScheduledEventAsync(baseExperience: 5, dayExperience: [2], experiencePoints: int.MaxValue - 1);

        var result = await CloseAsync(larpEvent, new ClosingAward(character.CharacterId, true, []));

        Assert.Equal(LarpEventOutcome.ExperienceOutOfRange, result.Outcome);
    }

    [Fact]
    public async Task CharacterEditStartedBeforeTheCloseIsStale()
    {
        var (larpEvent, character) = await ScheduledEventAsync(baseExperience: 5, dayExperience: [2]);
        await CloseAsync(larpEvent, new ClosingAward(character.CharacterId, true, []));

        var result = await _data.Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
            CharacterDefinitions.From(character) with { Title = "Captain" }, Token);

        Assert.Equal(RosterOutcome.StaleVersion, result.Outcome);
    }

    private async Task<(LarpEventSummary Event, CharacterSummary Character)> ScheduledEventAsync(
        int? baseExperience,
        int?[] dayExperience,
        int experiencePoints = 10)
    {
        var larpEvent = (await Events.CreateEventAsync(new LarpEventDefinition("Fort", null, baseExperience), Token)).Value!;
        for (var index = 0; index < dayExperience.Length; index++)
        {
            larpEvent = (await _data.Days.AddDayAsync(larpEvent.EventId, LarpEventTestData.FirstDay.AddDays(index),
                dayExperience[index], Token)).Value!;
        }

        var character = await _data.CreateCharacterAsync(experiencePoints: experiencePoints);
        await _data.Participants.RegisterAsync(larpEvent.EventId, character.CharacterId, LarpEventTestData.AllDays(larpEvent), Token);
        return (larpEvent, character);
    }

    private async Task<LarpEventResult<LarpEventSummary>> CloseAsync(LarpEventSummary larpEvent, params ClosingAward[] awards) =>
        await Events.CloseEventAsync(larpEvent.EventId, (await Events.GetEventAsync(larpEvent.EventId, Token))!.Version, awards, Token);

    private async Task<int> ExperienceOfAsync(CharacterSummary character) =>
        (await _data.Characters.GetCharacterAsync(character.CharacterId, Token))!.ExperiencePoints;
}
