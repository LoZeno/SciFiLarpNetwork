using JasperFx;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.Skills;
using SciFiLarpNetwork.Backend.Domain.Events.SkillEvents;
using SciFiLarpNetwork.Backend.Domain.Skills;
using SciFiLarpNetwork.Backend.Infrastructure.Skills.SkillDocuments;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Skills;

[Collection("Backend database integration")]
public sealed class SkillServiceTests : IAsyncLifetime
{
    private readonly AdministratorAuthenticationFactory _factory = new();

    private ISkillService Skills => _factory.Services.GetRequiredService<ISkillService>();

    private ITalentService Talents => _factory.Services.GetRequiredService<ITalentService>();

    private ICardFlagService CardFlags => _factory.Services.GetRequiredService<ICardFlagService>();

    public async ValueTask InitializeAsync()
    {
        await _factory.InitializeAsync();
        await _factory.Services.GetRequiredService<IDocumentStore>().Advanced.ResetAllData();
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task CreatedSkillKeepsEveryField()
    {
        var talent = await CreateTalentAsync("Engineering");
        var prerequisite = await CreateSkillAsync(Definition("Tinkering"));

        var result = await Skills.CreateSkillAsync(
            Definition("Hacking I", "Basic intrusion", 3, isMultiPurchase: true, isAdvanced: true) with
            {
                TalentIds = [talent.TalentId],
                GameFlags = [GameFlag.ImprovesHacking, GameFlag.AccessHacking],
                PrerequisiteGroups = [[prerequisite.SkillId]]
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(
            Snapshot(new SkillSummary(
                Guid.Empty, "Hacking I", "Basic intrusion", 3, true, true,
                [talent.TalentId], [GameFlag.AccessHacking, GameFlag.ImprovesHacking], [], [[prerequisite.SkillId]], false, 1)),
            Snapshot(result.Value!));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1000)]
    public async Task CostOutsideZeroTo999IsRejected(int cost)
    {
        var result = await Skills.CreateSkillAsync(Definition("Hacking I", cost: cost), TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.InvalidCost, result.Outcome);
    }

    [Fact]
    public async Task BlankSkillNameIsRejected()
    {
        var result = await Skills.CreateSkillAsync(Definition(" "), TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.InvalidName, result.Outcome);
    }

    [Fact]
    public async Task SkillNameIsUniqueAcrossTalents()
    {
        var engineering = await CreateTalentAsync("Engineering");
        var medicine = await CreateTalentAsync("Medicine");
        await CreateSkillAsync(Definition("First aid") with { TalentIds = [engineering.TalentId] });

        var result = await Skills.CreateSkillAsync(Definition("FIRST AID") with { TalentIds = [medicine.TalentId] }, TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.NameAlreadyUsed, result.Outcome);
    }

    [Fact]
    public async Task AssigningAnUnknownTalentIsRejected()
    {
        var result = await Skills.CreateSkillAsync(Definition("First aid") with { TalentIds = [Guid.NewGuid()] }, TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.UnknownTalent, result.Outcome);
    }

    [Fact]
    public async Task AssigningAnArchivedTalentIsRejected()
    {
        var talent = await CreateTalentAsync("Medicine");
        await Talents.ArchiveTalentAsync(talent.TalentId, TestContext.Current.CancellationToken);

        var result = await Skills.CreateSkillAsync(Definition("First aid") with { TalentIds = [talent.TalentId] }, TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.TalentArchived, result.Outcome);
    }

    [Fact]
    public async Task EditingASkillKeepsItsAssignmentToATalentArchivedLater()
    {
        var talent = await CreateTalentAsync("Medicine");
        var skill = await CreateSkillAsync(Definition("First aid") with { TalentIds = [talent.TalentId] });
        await Talents.ArchiveTalentAsync(talent.TalentId, TestContext.Current.CancellationToken);

        var result = await Skills.UpdateSkillAsync(
            skill.SkillId, skill.Version, Definition("First aid", "Bandages") with { TalentIds = [talent.TalentId] }, TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.Succeeded, result.Outcome);
    }

    [Fact]
    public async Task SkillInSeveralTalentsIsListedUnderEachOfThem()
    {
        var engineering = await CreateTalentAsync("Engineering");
        var medicine = await CreateTalentAsync("Medicine");
        await CreateSkillAsync(Definition("Chemistry") with { TalentIds = [engineering.TalentId, medicine.TalentId] });
        await CreateSkillAsync(Definition("Welding") with { TalentIds = [engineering.TalentId] });

        var medicineSkills = await Skills.GetSkillsAsync(medicine.TalentId, includeArchived: false, TestContext.Current.CancellationToken);

        Assert.Equal(["Chemistry"], medicineSkills.Select(skill => skill.Name));
    }

    [Fact]
    public async Task ArchivedSkillIsHiddenFromTheDefaultList()
    {
        var skill = await CreateSkillAsync(Definition("Chemistry"));
        await CreateSkillAsync(Definition("Welding"));

        await Skills.ArchiveSkillAsync(skill.SkillId, TestContext.Current.CancellationToken);

        Assert.Equal(["Welding"], (await Skills.GetSkillsAsync(null, includeArchived: false, TestContext.Current.CancellationToken)).Select(s => s.Name));
    }

    [Fact]
    public async Task UnarchivedSkillIsActiveAgain()
    {
        var skill = await CreateSkillAsync(Definition("Chemistry"));
        await Skills.ArchiveSkillAsync(skill.SkillId, TestContext.Current.CancellationToken);

        var result = await Skills.UnarchiveSkillAsync(skill.SkillId, TestContext.Current.CancellationToken);

        Assert.False(result.Value!.IsArchived);
    }

    [Fact]
    public async Task TypedCardFlagNameDefinesANewCardFlag()
    {
        await CreateSkillAsync(Definition("Toughness I") with { NewCardFlagNames = ["Toughness"] });

        var cardFlags = await CardFlags.GetCardFlagsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["Toughness"], cardFlags.Select(cardFlag => cardFlag.Name));
    }

    [Fact]
    public async Task TypedCardFlagNameMatchingAnExistingFlagReusesIt()
    {
        var first = await CreateSkillAsync(Definition("Toughness I") with { NewCardFlagNames = ["Toughness"] });

        var second = await CreateSkillAsync(Definition("Toughness II") with { NewCardFlagNames = ["TOUGHNESS"] });

        Assert.Equal(first.CardFlagIds, second.CardFlagIds);
    }

    [Fact]
    public async Task InvalidCardFlagNameIsRejected()
    {
        var result = await Skills.CreateSkillAsync(Definition("Toughness I") with { NewCardFlagNames = [" "] }, TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.InvalidCardFlagName, result.Outcome);
    }

    [Fact]
    public async Task UnknownCardFlagIsRejected()
    {
        var result = await Skills.CreateSkillAsync(Definition("Toughness I") with { CardFlagIds = [Guid.NewGuid()] }, TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.UnknownCardFlag, result.Outcome);
    }

    [Fact]
    public async Task UnknownPrerequisiteIsRejected()
    {
        var result = await Skills.CreateSkillAsync(Definition("Hacking II") with { PrerequisiteGroups = [[Guid.NewGuid()]] }, TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.UnknownPrerequisite, result.Outcome);
    }

    [Fact]
    public async Task EmptyPrerequisiteGroupIsRejected()
    {
        var result = await Skills.CreateSkillAsync(Definition("Hacking II") with { PrerequisiteGroups = [[]] }, TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.EmptyPrerequisiteGroup, result.Outcome);
    }

    [Fact]
    public async Task SkillRequiringItselfIsRejected()
    {
        var skill = await CreateSkillAsync(Definition("Hacking I"));

        var result = await Skills.UpdateSkillAsync(
            skill.SkillId, skill.Version, Definition("Hacking I") with { PrerequisiteGroups = [[skill.SkillId]] }, TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.SelfPrerequisite, result.Outcome);
    }

    [Fact]
    public async Task DirectPrerequisiteLoopIsRejected()
    {
        var first = await CreateSkillAsync(Definition("Hacking I"));
        var second = await CreateSkillAsync(Definition("Hacking II") with { PrerequisiteGroups = [[first.SkillId]] });

        var result = await Skills.UpdateSkillAsync(
            first.SkillId, first.Version, Definition("Hacking I") with { PrerequisiteGroups = [[second.SkillId]] }, TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.PrerequisiteLoop, result.Outcome);
    }

    [Fact]
    public async Task IndirectPrerequisiteLoopThroughAChoiceIsRejected()
    {
        var first = await CreateSkillAsync(Definition("Hacking I"));
        var other = await CreateSkillAsync(Definition("Electronics"));
        var second = await CreateSkillAsync(Definition("Hacking II") with { PrerequisiteGroups = [[other.SkillId, first.SkillId]] });
        var third = await CreateSkillAsync(Definition("Hacking III") with { PrerequisiteGroups = [[second.SkillId]] });

        var result = await Skills.UpdateSkillAsync(
            first.SkillId, first.Version, Definition("Hacking I") with { PrerequisiteGroups = [[third.SkillId]] }, TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.PrerequisiteLoop, result.Outcome);
    }

    [Fact]
    public async Task MandatoryAndChoicePrerequisitesAreStored()
    {
        var y = await CreateSkillAsync(Definition("Skill Y"));
        var z = await CreateSkillAsync(Definition("Skill Z"));
        var w = await CreateSkillAsync(Definition("Skill W"));
        var q = await CreateSkillAsync(Definition("Skill Q"));
        var x = await CreateSkillAsync(Definition("Skill X"));

        var result = await Skills.UpdateSkillAsync(
            x.SkillId,
            x.Version,
            Definition("Skill X") with { PrerequisiteGroups = [[y.SkillId], [z.SkillId], [w.SkillId, q.SkillId]] },
            TestContext.Current.CancellationToken);

        Assert.Equal(
            "[" + y.SkillId + "]|[" + z.SkillId + "]|[" + w.SkillId + "," + q.SkillId + "]",
            Groups(result.Value!.PrerequisiteGroups));
    }

    [Fact]
    public async Task ArchivedPrerequisiteIsAllowed()
    {
        var prerequisite = await CreateSkillAsync(Definition("Hacking I"));
        await Skills.ArchiveSkillAsync(prerequisite.SkillId, TestContext.Current.CancellationToken);

        var result = await Skills.CreateSkillAsync(Definition("Hacking II") with { PrerequisiteGroups = [[prerequisite.SkillId]] }, TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.Succeeded, result.Outcome);
    }

    [Fact]
    public async Task EditingOnlyTheDescriptionKeepsRelationships()
    {
        var talent = await CreateTalentAsync("Engineering");
        var prerequisite = await CreateSkillAsync(Definition("Tinkering"));
        var skill = await CreateSkillAsync(Definition("Welding") with
        {
            TalentIds = [talent.TalentId],
            GameFlags = [GameFlag.AccessTechnicalWorkbench],
            NewCardFlagNames = ["Strength"],
            PrerequisiteGroups = [[prerequisite.SkillId]]
        });

        var result = await Skills.UpdateSkillAsync(
            skill.SkillId,
            skill.Version,
            Definition("Welding", "Joins metal") with
            {
                TalentIds = skill.TalentIds,
                GameFlags = skill.GameFlags,
                CardFlagIds = skill.CardFlagIds,
                PrerequisiteGroups = skill.PrerequisiteGroups
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(
            Snapshot(skill with { Description = "Joins metal", Version = 2 }),
            Snapshot(result.Value!));
    }

    [Fact]
    public async Task SkillUpdateFromAStaleVersionIsRejected()
    {
        var skill = await CreateSkillAsync(Definition("Welding"));
        await Skills.UpdateSkillAsync(skill.SkillId, skill.Version, Definition("Welding", "Joins metal"), TestContext.Current.CancellationToken);

        var result = await Skills.UpdateSkillAsync(skill.SkillId, skill.Version, Definition("Welding", "Melts metal"), TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.StaleVersion, result.Outcome);
    }

    [Fact]
    public async Task RenamingToAnotherSkillsNameIsRejected()
    {
        await CreateSkillAsync(Definition("Welding"));
        var skill = await CreateSkillAsync(Definition("Tinkering"));

        var result = await Skills.UpdateSkillAsync(skill.SkillId, skill.Version, Definition("welding"), TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.NameAlreadyUsed, result.Outcome);
    }

    [Fact]
    public async Task UpdatingAnUnknownSkillReportsNotFound()
    {
        var result = await Skills.UpdateSkillAsync(Guid.NewGuid(), 1, Definition("Welding"), TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task UnchangedSkillUpdateKeepsTheVersion()
    {
        var skill = await CreateSkillAsync(Definition("Welding"));

        var result = await Skills.UpdateSkillAsync(skill.SkillId, skill.Version, Definition("Welding"), TestContext.Current.CancellationToken);

        Assert.Equal(1L, result.Value!.Version);
    }

    [Fact]
    public async Task ADuplicateNormalizedSkillNameIsRejectedAsAnExistingDocument()
    {
        await CreateSkillAsync(Definition("Welding"));
        var skillId = Guid.NewGuid();
        await using var session = _factory.Services.GetRequiredService<IDocumentStore>().LightweightSession();
        session.Events.StartStream<SkillDocument>(
            skillId,
            new SkillCreated(skillId, "WELDING", CatalogNames.Normalize("WELDING"), null, 1, false, false, [], [], [], [], DateTimeOffset.UtcNow));

        var exception = await Record.ExceptionAsync(() => session.SaveChangesAsync(TestContext.Current.CancellationToken));

        Assert.IsType<DocumentAlreadyExistsException>(exception);
    }

    [Fact]
    public async Task ParallelCreatesWithTheSameNameLetOnlyOneSucceed()
    {
        var results = await Task.WhenAll(
            Skills.CreateSkillAsync(Definition("Welding"), TestContext.Current.CancellationToken),
            Skills.CreateSkillAsync(Definition("WELDING"), TestContext.Current.CancellationToken));

        Assert.Equal(
            [CatalogOutcome.Succeeded, CatalogOutcome.NameAlreadyUsed],
            results.Select(result => result.Outcome).Order());
    }

    [Fact]
    public async Task ParallelEditsThatWouldCloseALoopLetOnlyOneSucceed()
    {
        var first = await CreateSkillAsync(Definition("Hacking I"));
        var second = await CreateSkillAsync(Definition("Hacking II"));

        var results = await Task.WhenAll(
            Skills.UpdateSkillAsync(
                first.SkillId,
                first.Version,
                Definition("Hacking I") with { PrerequisiteGroups = [[second.SkillId]] },
                TestContext.Current.CancellationToken),
            Skills.UpdateSkillAsync(
                second.SkillId,
                second.Version,
                Definition("Hacking II") with { PrerequisiteGroups = [[first.SkillId]] },
                TestContext.Current.CancellationToken));

        Assert.Equal(["Rejected", "Succeeded"], results.Select(result => LoopRaceOutcome(result.Outcome)).Order());
    }

    /// <summary>
    /// A loop edit that loses the race is rejected either by validation (it saw the other edit) or by
    /// Marten's consistency check on the prerequisite streams it read (it did not).
    /// </summary>
    private static string LoopRaceOutcome(CatalogOutcome outcome) =>
        outcome is CatalogOutcome.PrerequisiteLoop or CatalogOutcome.StaleVersion ? "Rejected" : outcome.ToString();
    private static SkillDefinition Definition(
        string name,
        string? description = null,
        int cost = 1,
        bool isMultiPurchase = false,
        bool isAdvanced = false) =>
        new(name, description, cost, isMultiPurchase, isAdvanced, [], [], [], [], []);

    private static object Snapshot(SkillSummary skill) =>
        new
        {
            skill.Name,
            skill.Description,
            skill.Cost,
            skill.IsMultiPurchase,
            skill.IsAdvanced,
            TalentIds = string.Join(',', skill.TalentIds),
            GameFlags = string.Join(',', skill.GameFlags),
            CardFlagIds = string.Join(',', skill.CardFlagIds),
            PrerequisiteGroups = Groups(skill.PrerequisiteGroups),
            skill.IsArchived,
            skill.Version
        };

    private static string Groups(IReadOnlyList<IReadOnlyList<Guid>> groups) =>
        string.Join('|', groups.Select(group => "[" + string.Join(',', group) + "]"));

    private async Task<TalentSummary> CreateTalentAsync(string name) =>
        (await Talents.CreateTalentAsync(new TalentDefinition(name, null), TestContext.Current.CancellationToken)).Value!;

    private async Task<SkillSummary> CreateSkillAsync(SkillDefinition definition) =>
        (await Skills.CreateSkillAsync(definition, TestContext.Current.CancellationToken)).Value!;
}
