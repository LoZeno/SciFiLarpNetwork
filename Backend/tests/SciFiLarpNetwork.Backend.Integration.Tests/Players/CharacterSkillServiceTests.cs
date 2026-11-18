using Marten;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Application.Skills;
using SciFiLarpNetwork.Backend.Domain.CharacterSkills;
using SciFiLarpNetwork.Backend.Domain.Events.CharacterEvents;
using SciFiLarpNetwork.Backend.Domain.Players;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Players;

[Collection("Backend database integration")]
public sealed class CharacterSkillServiceTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private IPlayerService Players => factory.Services.GetRequiredService<IPlayerService>();

    private ICharacterService Characters => factory.Services.GetRequiredService<ICharacterService>();

    private ICharacterSkillService CharacterSkills => factory.Services.GetRequiredService<ICharacterSkillService>();

    private ITalentService Talents => factory.Services.GetRequiredService<ITalentService>();

    private ISkillService Skills => factory.Services.GetRequiredService<ISkillService>();

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task NewCharacterHasNoTalentsOrSkillsAndEveryPointFree()
    {
        var character = await CreateCharacterAsync(experiencePoints: 30);

        var skills = await CharacterSkills.GetCharacterSkillsAsync(character.CharacterId, TestContext.Current.CancellationToken);

        Assert.Equivalent(
            new CharacterSkillsSummary(character.CharacterId, "Kira", 30, 30, [], [], character.Version),
            skills,
            strict: true);
    }

    [Fact]
    public async Task SavedTalentsAndSkillsAreReadBackWithFreePoints()
    {
        var character = await CreateCharacterAsync(experiencePoints: 30);
        var pilot = await CreateTalentAsync("Pilot");
        var medic = await CreateTalentAsync("Medic");
        var flying = await CreateSkillAsync("Flying", 10, [pilot.TalentId]);
        var stamina = await CreateSkillAsync("Stamina", 5, [medic.TalentId], isMultiPurchase: true);

        await SaveAsync(character, [medic.TalentId, pilot.TalentId],
            [new SkillPurchase(stamina.SkillId, medic.TalentId, 2, null), new SkillPurchase(flying.SkillId, pilot.TalentId, 1, " Shuttles ")]);

        Assert.Equivalent(
            new CharacterSkillsSummary(
                character.CharacterId,
                "Kira",
                30,
                10,
                [new CharacterTalentSummary(medic.TalentId, "Medic", false), new CharacterTalentSummary(pilot.TalentId, "Pilot", false)],
                [
                    new CharacterSkillPurchaseSummary(flying.SkillId, "Flying", pilot.TalentId, 1, "Shuttles", 10, false, false),
                    new CharacterSkillPurchaseSummary(stamina.SkillId, "Stamina", medic.TalentId, 2, null, 5, true, false)
                ],
                character.Version + 1),
            await CharacterSkills.GetCharacterSkillsAsync(character.CharacterId, TestContext.Current.CancellationToken),
            strict: true);
    }

    [Fact]
    public async Task SaveRecordsTheReplacedListsAsOneEvent()
    {
        var character = await CreateCharacterAsync(experiencePoints: 30);
        var pilot = await CreateTalentAsync("Pilot");
        var flying = await CreateSkillAsync("Flying", 10, [pilot.TalentId]);

        await SaveAsync(character, [pilot.TalentId], [new SkillPurchase(flying.SkillId, pilot.TalentId, 1, " Shuttles ")]);

        await using var session = factory.Services.GetRequiredService<IDocumentStore>().QuerySession();
        var events = await session.Events.FetchStreamAsync(character.CharacterId, token: TestContext.Current.CancellationToken);
        Assert.Equivalent(
            new
            {
                character.CharacterId,
                TalentIds = new[] { pilot.TalentId },
                Purchases = new[] { new SkillPurchase(flying.SkillId, pilot.TalentId, 1, "Shuttles") }
            },
            events[^1].Data as CharacterSkillsChanged);
    }

    [Fact]
    public async Task UnchangedSaveKeepsTheCharacterVersion()
    {
        var character = await CreateCharacterAsync(experiencePoints: 30);

        var result = await SaveAsync(character, [], []);

        Assert.Equal(character.Version, result.Value!.Version);
    }

    [Fact]
    public async Task InvalidListsAreRejectedAndNothingIsStored()
    {
        var character = await CreateCharacterAsync(experiencePoints: 30);
        var pilot = await CreateTalentAsync("Pilot");
        var flying = await CreateSkillAsync("Flying", 10, [pilot.TalentId]);

        var result = await SaveAsync(character, [pilot.TalentId], [new SkillPurchase(flying.SkillId, pilot.TalentId, 0, null)]);

        var stored = await CharacterSkills.GetCharacterSkillsAsync(character.CharacterId, TestContext.Current.CancellationToken);
        Assert.Equal(new { Outcome = RosterOutcome.InvalidPurchaseCount, Version = character.Version },
            new { result.Outcome, stored!.Version });
    }

    [Fact]
    public async Task RemovingANeededSkillIsRejectedNamingTheSkillsThatNeedIt()
    {
        var character = await CreateCharacterAsync(experiencePoints: 100);
        var pilot = await CreateTalentAsync("Pilot");
        var flying = await CreateSkillAsync("Flying", 10, [pilot.TalentId]);
        var aces = await CreateSkillAsync("Aces", 10, [pilot.TalentId], prerequisites: [[flying.SkillId]]);
        var stunts = await CreateSkillAsync("Stunts", 10, [pilot.TalentId], prerequisites: [[flying.SkillId]]);
        var owned = await SaveAsync(character, [pilot.TalentId],
        [
            new SkillPurchase(flying.SkillId, pilot.TalentId, 1, null),
            new SkillPurchase(stunts.SkillId, pilot.TalentId, 1, null),
            new SkillPurchase(aces.SkillId, pilot.TalentId, 1, null)
        ]);

        var result = await CharacterSkills.SaveCharacterSkillsAsync(character.CharacterId, owned.Value!.Version,
            new CharacterSkillsDefinition([pilot.TalentId],
            [
                new SkillPurchase(stunts.SkillId, pilot.TalentId, 1, null),
                new SkillPurchase(aces.SkillId, pilot.TalentId, 1, null)
            ]),
            TestContext.Current.CancellationToken);

        Assert.Equivalent(
            new { Outcome = RosterOutcome.RequiredByOtherSkills, Value = (CharacterSkillsSummary?)null, SkillNames = (string[])["Aces", "Stunts"] },
            result,
            strict: true);
    }

    [Fact]
    public async Task NewSkillIsCheckedAgainstTheStoredCatalog()
    {
        var character = await CreateCharacterAsync(experiencePoints: 100);
        var pilot = await CreateTalentAsync("Pilot");
        var medic = await CreateTalentAsync("Medic");
        var flying = await CreateSkillAsync("Flying", 10, [pilot.TalentId]);

        var result = await SaveAsync(character, [pilot.TalentId, medic.TalentId],
            [new SkillPurchase(flying.SkillId, medic.TalentId, 1, null)]);

        Assert.Equivalent(new { Outcome = RosterOutcome.SkillNotInTalent, SkillNames = (string[])["Flying"] }, result);
    }

    [Fact]
    public async Task EditBasedOnAnOlderCharacterVersionIsStale()
    {
        var character = await CreateCharacterAsync(experiencePoints: 30);
        await Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
            CharacterDefinitions.From(character) with { ExperiencePoints = 40 }, TestContext.Current.CancellationToken);

        var result = await SaveAsync(character, [], []);

        Assert.Equal(RosterOutcome.StaleVersion, result.Outcome);
    }

    [Fact]
    public async Task SavingForAnUnknownCharacterIsNotFound()
    {
        var result = await CharacterSkills.SaveCharacterSkillsAsync(Guid.NewGuid(), 1, new CharacterSkillsDefinition([], []),
            TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task UnknownCharacterHasNoSkills()
    {
        var skills = await CharacterSkills.GetCharacterSkillsAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Null(skills);
    }

    [Fact]
    public async Task RaisingAnOwnedSkillsCostMakesFreePointsNegative()
    {
        var character = await CreateCharacterAsync(experiencePoints: 10);
        var pilot = await CreateTalentAsync("Pilot");
        var flying = await CreateSkillAsync("Flying", 10, [pilot.TalentId]);
        await SaveAsync(character, [pilot.TalentId], [new SkillPurchase(flying.SkillId, pilot.TalentId, 1, null)]);

        await Skills.UpdateSkillAsync(flying.SkillId, flying.Version, SkillDefinitionFor("Flying", 15, [pilot.TalentId]),
            TestContext.Current.CancellationToken);

        var skills = await CharacterSkills.GetCharacterSkillsAsync(character.CharacterId, TestContext.Current.CancellationToken);
        Assert.Equal(-5, skills!.FreePoints);
    }

    [Fact]
    public async Task LoweringExperiencePointsBelowTheSpentPointsIsRejected()
    {
        var character = await CreateCharacterWithFlyingAsync(experiencePoints: 30, cost: 20);

        var result = await Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
            CharacterDefinitions.From(character) with { ExperiencePoints = 19 }, TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.InsufficientFreePoints, result.Outcome);
    }

    [Fact]
    public async Task LoweringExperiencePointsToTheSpentPointsIsAccepted()
    {
        var character = await CreateCharacterWithFlyingAsync(experiencePoints: 30, cost: 20);

        var result = await Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
            CharacterDefinitions.From(character) with { ExperiencePoints = 20 }, TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.Succeeded, result.Outcome);
    }

    [Fact]
    public async Task OverlappingSkillSaveAndCharacterEditLetOnlyOneSucceed()
    {
        var character = await CreateCharacterAsync(experiencePoints: 30);
        var pilot = await CreateTalentAsync("Pilot");
        using var overlap = new Barrier(2);
        factory.Clock.ReadBarrier = overlap;

        var outcomes = await Task.WhenAll(
            Task.Run(async () => (await CharacterSkills.SaveCharacterSkillsAsync(character.CharacterId, character.Version,
                new CharacterSkillsDefinition([pilot.TalentId], []), TestContext.Current.CancellationToken)).Outcome),
            Task.Run(async () => (await Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
                CharacterDefinitions.From(character) with { ExperiencePoints = 40 }, TestContext.Current.CancellationToken)).Outcome));
        factory.Clock.ReadBarrier = null;

        Assert.Equal([RosterOutcome.Succeeded, RosterOutcome.StaleVersion], outcomes.Order());
    }

    private async Task<CharacterSummary> CreateCharacterWithFlyingAsync(int experiencePoints, int cost)
    {
        var character = await CreateCharacterAsync(experiencePoints);
        var pilot = await CreateTalentAsync("Pilot");
        var flying = await CreateSkillAsync("Flying", cost, [pilot.TalentId]);
        await SaveAsync(character, [pilot.TalentId], [new SkillPurchase(flying.SkillId, pilot.TalentId, 1, null)]);
        return (await Characters.GetCharacterAsync(character.CharacterId, TestContext.Current.CancellationToken))!;
    }

    private Task<CharacterSkillsResult> SaveAsync(
        CharacterSummary character,
        IReadOnlyList<Guid> talentIds,
        IReadOnlyList<SkillPurchase> purchases) =>
        CharacterSkills.SaveCharacterSkillsAsync(character.CharacterId, character.Version,
            new CharacterSkillsDefinition(talentIds, purchases), TestContext.Current.CancellationToken);

    private async Task<CharacterSummary> CreateCharacterAsync(int experiencePoints)
    {
        var player = (await Players.CreatePlayerAsync(new PlayerDefinition("Ann", null, null, null, PlayerType.Player),
            TestContext.Current.CancellationToken)).Value!;
        return (await Characters.CreateCharacterAsync(player.PlayerId,
            CharacterDefinitions.Named("Kira") with { ExperiencePoints = experiencePoints },
            TestContext.Current.CancellationToken)).Value!;
    }

    private async Task<TalentSummary> CreateTalentAsync(string name) =>
        (await Talents.CreateTalentAsync(new TalentDefinition(name, null), TestContext.Current.CancellationToken)).Value!;

    private async Task<SkillSummary> CreateSkillAsync(
        string name,
        int cost,
        IReadOnlyList<Guid> talentIds,
        bool isMultiPurchase = false,
        IReadOnlyList<IReadOnlyList<Guid>>? prerequisites = null) =>
        (await Skills.CreateSkillAsync(SkillDefinitionFor(name, cost, talentIds, isMultiPurchase, prerequisites),
            TestContext.Current.CancellationToken)).Value!;

    private static SkillDefinition SkillDefinitionFor(
        string name,
        int cost,
        IReadOnlyList<Guid> talentIds,
        bool isMultiPurchase = false,
        IReadOnlyList<IReadOnlyList<Guid>>? prerequisites = null) =>
        new(name, null, cost, isMultiPurchase, false, talentIds, [], [], [], prerequisites ?? []);
}
