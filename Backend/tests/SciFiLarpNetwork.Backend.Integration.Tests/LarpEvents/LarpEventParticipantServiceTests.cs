using SciFiLarpNetwork.Backend.Application.LarpEvents;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Characters;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;
using SciFiLarpNetwork.Backend.Tests.Integration.Players;

namespace SciFiLarpNetwork.Backend.Tests.Integration.LarpEvents;

[Collection("Backend database integration")]
public sealed class LarpEventParticipantServiceTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private readonly LarpEventTestData _data = new(factory);

    private ILarpEventParticipantService Participants => _data.Participants;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task RegisteredCharacterIsListedWithItsDays()
    {
        var created = await _data.CreateEventAsync(dayCount: 2);
        var character = await _data.CreateCharacterAsync(experiencePoints: 7);

        var result = await Participants.RegisterAsync(created.EventId, character.CharacterId, LarpEventTestData.AllDays(created), Token);

        Assert.Equivalent(
            new ParticipantSummary(
                character.CharacterId,
                character.Number,
                "Kira",
                character.PlayerId,
                "Ann",
                CharacterStatus.Active,
                false,
                7,
                LarpEventTestData.AllDays(created),
                [LarpEventTestData.FirstDay, LarpEventTestData.FirstDay.AddDays(1)],
                0,
                [],
                false,
                []),
            result.Value,
            strict: true);
    }

    [Fact]
    public async Task RegisteringForMoreDaysAddsOnlyTheNewOnes()
    {
        var created = await _data.CreateEventAsync(dayCount: 2);
        var character = await _data.CreateCharacterAsync();
        await Participants.RegisterAsync(created.EventId, character.CharacterId, [created.Days[0].DayId], Token);

        var result = await Participants.RegisterAsync(created.EventId, character.CharacterId, LarpEventTestData.AllDays(created), Token);

        Assert.Equal(LarpEventTestData.AllDays(created), result.Value!.DayIds);
    }

    [Fact]
    public async Task RegisteringOnlyForRegisteredDaysIsRejected()
    {
        var created = await _data.CreateEventAsync(dayCount: 1);
        var character = await _data.CreateCharacterAsync();
        await Participants.RegisterAsync(created.EventId, character.CharacterId, LarpEventTestData.AllDays(created), Token);

        var result = await Participants.RegisterAsync(created.EventId, character.CharacterId, LarpEventTestData.AllDays(created), Token);

        Assert.Equal(LarpEventOutcome.AlreadyRegistered, result.Outcome);
    }

    [Fact]
    public async Task RetiredCharacterCannotBeRegistered()
    {
        var created = await _data.CreateEventAsync(dayCount: 1);
        var character = await _data.CreateCharacterAsync();
        await _data.Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
            CharacterDefinitions.From(character) with { Status = CharacterStatus.Retired }, Token);

        var result = await Participants.RegisterAsync(created.EventId, character.CharacterId, LarpEventTestData.AllDays(created), Token);

        Assert.Equal(LarpEventOutcome.CharacterNotActive, result.Outcome);
    }

    [Fact]
    public async Task CharacterRetiredAfterRegisteringStaysRegistered()
    {
        var created = await _data.CreateEventAsync(dayCount: 1);
        var character = await _data.CreateCharacterAsync();
        await Participants.RegisterAsync(created.EventId, character.CharacterId, LarpEventTestData.AllDays(created), Token);
        await _data.Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
            CharacterDefinitions.From(character) with { Status = CharacterStatus.Retired }, Token);

        var result = await Participants.GetParticipantsAsync(created.EventId, null, Token);

        Assert.Equal([CharacterStatus.Retired], result.Value!.Select(participant => participant.Status));
    }

    [Fact]
    public async Task RegistrationWithoutDaysIsRejected()
    {
        var created = await _data.CreateEventAsync(dayCount: 1);
        var character = await _data.CreateCharacterAsync();

        var result = await Participants.RegisterAsync(created.EventId, character.CharacterId, [], Token);

        Assert.Equal(LarpEventOutcome.NoDaysSelected, result.Outcome);
    }

    [Fact]
    public async Task RegistrationForAnUnknownDayIsRejected()
    {
        var created = await _data.CreateEventAsync(dayCount: 1);
        var character = await _data.CreateCharacterAsync();

        var result = await Participants.RegisterAsync(created.EventId, character.CharacterId, [Guid.NewGuid()], Token);

        Assert.Equal(LarpEventOutcome.UnknownDay, result.Outcome);
    }

    [Fact]
    public async Task UnknownCharacterCannotBeRegistered()
    {
        var created = await _data.CreateEventAsync(dayCount: 1);

        var result = await Participants.RegisterAsync(created.EventId, Guid.NewGuid(), LarpEventTestData.AllDays(created), Token);

        Assert.Equal(LarpEventOutcome.UnknownCharacter, result.Outcome);
    }

    [Fact]
    public async Task ClosedEventTakesNoRegistrations()
    {
        var created = await _data.CreateEventAsync(dayCount: 1);
        var character = await _data.CreateCharacterAsync();
        await _data.CloseAsync(created);

        var result = await Participants.RegisterAsync(created.EventId, character.CharacterId, LarpEventTestData.AllDays(created), Token);

        Assert.Equal(LarpEventOutcome.EventClosed, result.Outcome);
    }

    [Fact]
    public async Task DayListShowsOnlyThatDaysCharacters()
    {
        var created = await _data.CreateEventAsync(dayCount: 2);
        var kira = await _data.CreateCharacterAsync("Kira");
        var odo = await _data.CreateCharacterAsync("Odo");
        await Participants.RegisterAsync(created.EventId, kira.CharacterId, [created.Days[0].DayId], Token);
        await Participants.RegisterAsync(created.EventId, odo.CharacterId, [created.Days[1].DayId], Token);

        var result = await Participants.GetParticipantsAsync(created.EventId, created.Days[1].DayId, Token);

        Assert.Equal(["Odo"], result.Value!.Select(participant => participant.CharacterName));
    }

    [Fact]
    public async Task UnregisteringFromEveryDayRemovesTheCharacter()
    {
        var created = await _data.CreateEventAsync(dayCount: 1);
        var character = await _data.CreateCharacterAsync();
        await Participants.RegisterAsync(created.EventId, character.CharacterId, LarpEventTestData.AllDays(created), Token);

        var result = await Participants.UnregisterAsync(created.EventId, character.CharacterId, LarpEventTestData.AllDays(created), Token);

        Assert.Equal(0, result.Value!.ParticipantCount);
    }

    [Fact]
    public async Task UnregisteringFromAnUnregisteredDayIsRejected()
    {
        var created = await _data.CreateEventAsync(dayCount: 2);
        var character = await _data.CreateCharacterAsync();
        await Participants.RegisterAsync(created.EventId, character.CharacterId, [created.Days[0].DayId], Token);

        var result = await Participants.UnregisterAsync(created.EventId, character.CharacterId, [created.Days[1].DayId], Token);

        Assert.Equal(LarpEventOutcome.NotRegistered, result.Outcome);
    }

    [Fact]
    public async Task ExperienceChangeUpdatesTheCharacter()
    {
        var (created, character) = await RegisteredCharacterAsync(experiencePoints: 10);

        await Participants.ChangeExperienceAsync(created.EventId, character.CharacterId, 5, Token);

        Assert.Equal(15, (await _data.Characters.GetCharacterAsync(character.CharacterId, Token))!.ExperiencePoints);
    }

    [Fact]
    public async Task ExperienceChangesAreRecordedAgainstTheEvent()
    {
        var (created, character) = await RegisteredCharacterAsync(experiencePoints: 10);
        await Participants.ChangeExperienceAsync(created.EventId, character.CharacterId, 5, Token);

        var result = await Participants.ChangeExperienceAsync(created.EventId, character.CharacterId, -2, Token);

        Assert.Equal(
            new { Total = 3L, Amounts = "5,-2" },
            new { Total = result.Value!.ExperienceTotal, Amounts = string.Join(',', result.Value.ExperienceChanges.Select(change => change.Amount)) });
    }

    [Fact]
    public async Task ExperienceChangeRecordsWhenItWasGiven()
    {
        var (created, character) = await RegisteredCharacterAsync(experiencePoints: 10);
        factory.Clock.Advance(TimeSpan.FromDays(3));
        var before = factory.Clock.GetUtcNow();

        var result = await Participants.ChangeExperienceAsync(created.EventId, character.CharacterId, 5, Token);

        Assert.InRange(result.Value!.ExperienceChanges[0].ChangedAtUtc, before, factory.Clock.GetUtcNow());
    }

    [Fact]
    public async Task ZeroExperienceChangeIsRejected()
    {
        var (created, character) = await RegisteredCharacterAsync(experiencePoints: 10);

        var result = await Participants.ChangeExperienceAsync(created.EventId, character.CharacterId, 0, Token);

        Assert.Equal(LarpEventOutcome.InvalidExperienceChange, result.Outcome);
    }

    [Fact]
    public async Task ExperienceCannotGoBelowZero()
    {
        var (created, character) = await RegisteredCharacterAsync(experiencePoints: 10);

        var result = await Participants.ChangeExperienceAsync(created.EventId, character.CharacterId, -11, Token);

        Assert.Equal(LarpEventOutcome.ExperienceOutOfRange, result.Outcome);
    }

    [Fact]
    public async Task ExperienceCannotDropBelowTheSpentPoints()
    {
        var (created, character) = await RegisteredCharacterAsync(experiencePoints: 10);
        await _data.BuySkillAsync(character, cost: 8);

        var result = await Participants.ChangeExperienceAsync(created.EventId, character.CharacterId, -3, Token);

        Assert.Equal(LarpEventOutcome.InsufficientFreePoints, result.Outcome);
    }

    [Fact]
    public async Task RejectedExperienceChangeLeavesTheCharacterUnchanged()
    {
        var (created, character) = await RegisteredCharacterAsync(experiencePoints: 10);

        await Participants.ChangeExperienceAsync(created.EventId, character.CharacterId, -11, Token);

        Assert.Equal(10, (await _data.Characters.GetCharacterAsync(character.CharacterId, Token))!.ExperiencePoints);
    }

    [Fact]
    public async Task UnregisteredCharacterGetsNoExperience()
    {
        var created = await _data.CreateEventAsync(dayCount: 1);
        var character = await _data.CreateCharacterAsync();

        var result = await Participants.ChangeExperienceAsync(created.EventId, character.CharacterId, 5, Token);

        Assert.Equal(LarpEventOutcome.NotRegistered, result.Outcome);
    }

    [Fact]
    public async Task ClosedEventGivesNoExperience()
    {
        var (created, character) = await RegisteredCharacterAsync(experiencePoints: 10);
        await _data.CloseAsync(created);

        var result = await Participants.ChangeExperienceAsync(created.EventId, character.CharacterId, 5, Token);

        Assert.Equal(LarpEventOutcome.EventClosed, result.Outcome);
    }

    [Fact]
    public async Task CharacterEditStartedBeforeAnExperienceChangeIsStale()
    {
        var (created, character) = await RegisteredCharacterAsync(experiencePoints: 10);
        await Participants.ChangeExperienceAsync(created.EventId, character.CharacterId, 5, Token);

        var result = await _data.Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
            CharacterDefinitions.From(character) with { Title = "Captain" }, Token);

        Assert.Equal(RosterOutcome.StaleVersion, result.Outcome);
    }

    private async Task<(LarpEventSummary Event, CharacterSummary Character)> RegisteredCharacterAsync(int experiencePoints)
    {
        var created = await _data.CreateEventAsync(dayCount: 1);
        var character = await _data.CreateCharacterAsync(experiencePoints: experiencePoints);
        await Participants.RegisterAsync(created.EventId, character.CharacterId, LarpEventTestData.AllDays(created), Token);
        return (created, character);
    }
}
