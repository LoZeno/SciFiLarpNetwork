using SciFiLarpNetwork.Backend.Domain.CharacterSkills;

namespace SciFiLarpNetwork.Backend.Tests.Domain.CharacterSkills;

public sealed class CharacterSkillRulesTests
{
    private static readonly Guid Pilot = Guid.NewGuid();
    private static readonly Guid Medic = Guid.NewGuid();
    private static readonly Guid Retired = Guid.NewGuid();
    private static readonly Guid Flying = Guid.NewGuid();
    private static readonly Guid Gunnery = Guid.NewGuid();
    private static readonly Guid Aces = Guid.NewGuid();
    private static readonly Guid FirstAid = Guid.NewGuid();
    private static readonly Guid Stamina = Guid.NewGuid();
    private static readonly Guid Relic = Guid.NewGuid();

    private static readonly Dictionary<Guid, TalentFacts> Talents = new()
    {
        [Pilot] = new TalentFacts(Pilot, false),
        [Medic] = new TalentFacts(Medic, false),
        [Retired] = new TalentFacts(Retired, true)
    };

    private static readonly Dictionary<Guid, SkillFacts> Skills = new()
    {
        [Flying] = Skill(Flying, cost: 10, talents: [Pilot]),
        [Gunnery] = Skill(Gunnery, cost: 10, talents: [Pilot]),
        [Aces] = Skill(Aces, cost: 20, talents: [Pilot], prerequisites: [[Flying], [Gunnery, FirstAid]]),
        [FirstAid] = Skill(FirstAid, cost: 5, talents: [Medic, Pilot]),
        [Stamina] = Skill(Stamina, cost: 5, talents: [Medic], isMultiPurchase: true),
        [Relic] = Skill(Relic, cost: 0, talents: [Pilot], isArchived: true)
    };

    [Fact]
    public void FreePointsSubtractEachCostTimesItsPurchases()
    {
        var freePoints = CharacterSkillRules.FreePoints(100, [Buy(Flying, Pilot), Buy(Stamina, Medic, count: 3)],
            skillId => Skills[skillId].Cost);

        Assert.Equal(75, freePoints);
    }

    [Fact]
    public void BuyingThroughOwnedTalentsWithinPointsIsAccepted()
    {
        var finding = Check(talents: [Pilot, Medic], purchases: [Buy(Flying, Pilot), Buy(Stamina, Medic, count: 2)]);

        Assert.Equal(CharacterSkillFinding.None, finding);
    }

    [Fact]
    public void UnknownNewTalentIsRejected()
    {
        var finding = Check(talents: [Guid.NewGuid()], purchases: []);

        Assert.Equal(CharacterSkillProblem.UnknownTalent, finding.Problem);
    }

    [Fact]
    public void ArchivedNewTalentIsRejected()
    {
        var finding = Check(talents: [Retired], purchases: []);

        Assert.Equal(CharacterSkillProblem.TalentArchived, finding.Problem);
    }

    [Fact]
    public void TalentArchivedAfterItWasPickedIsKept()
    {
        var finding = Check(currentTalents: [Retired], talents: [Retired, Pilot], purchases: []);

        Assert.Equal(CharacterSkillFinding.None, finding);
    }

    [Fact]
    public void UnknownSkillIsRejectedWithItsIdentifier()
    {
        var unknown = Guid.NewGuid();

        var finding = Check(talents: [Pilot], purchases: [Buy(unknown, Pilot)]);

        Assert.Equivalent(new { Problem = CharacterSkillProblem.UnknownSkill, SkillIds = new[] { unknown } }, finding, strict: true);
    }

    [Fact]
    public void SkillBoughtThroughATalentTheCharacterDoesNotHaveIsRejected()
    {
        var finding = Check(talents: [Medic], purchases: [Buy(Flying, Pilot)]);

        Assert.Equivalent(new { Problem = CharacterSkillProblem.TalentNotOwned, SkillIds = new[] { Flying } }, finding, strict: true);
    }

    [Fact]
    public void RemovingATalentStillUsedByASkillIsRejected()
    {
        var finding = Check(currentTalents: [Pilot], currentPurchases: [Buy(Flying, Pilot)], talents: [],
            purchases: [Buy(Flying, Pilot)]);

        Assert.Equal(CharacterSkillProblem.TalentNotOwned, finding.Problem);
    }

    [Fact]
    public void SkillBoughtThroughATalentItDoesNotBelongToIsRejected()
    {
        var finding = Check(talents: [Medic], purchases: [Buy(Flying, Medic)]);

        Assert.Equivalent(new { Problem = CharacterSkillProblem.SkillNotInTalent, SkillIds = new[] { Flying } }, finding, strict: true);
    }

    [Fact]
    public void SkillMovedToAnotherTalentItBelongsToIsAccepted()
    {
        var finding = Check(currentTalents: [Pilot, Medic], currentPurchases: [Buy(FirstAid, Pilot)], talents: [Pilot, Medic],
            purchases: [Buy(FirstAid, Medic)]);

        Assert.Equal(CharacterSkillFinding.None, finding);
    }

    [Fact]
    public void SkillMovedToATalentItDoesNotBelongToIsRejected()
    {
        var finding = Check(currentTalents: [Pilot, Medic], currentPurchases: [Buy(Flying, Pilot)], talents: [Pilot, Medic],
            purchases: [Buy(Flying, Medic)]);

        Assert.Equal(CharacterSkillProblem.SkillNotInTalent, finding.Problem);
    }

    [Fact]
    public void SkillTakenOffItsTalentAfterPurchaseIsKept()
    {
        var finding = Check(currentTalents: [Medic], currentPurchases: [Buy(Flying, Medic)], talents: [Medic],
            purchases: [Buy(Flying, Medic)]);

        Assert.Equal(CharacterSkillFinding.None, finding);
    }

    [Fact]
    public void ArchivedSkillCannotBeBought()
    {
        var finding = Check(talents: [Pilot], purchases: [Buy(Relic, Pilot)]);

        Assert.Equal(CharacterSkillProblem.SkillArchived, finding.Problem);
    }

    [Fact]
    public void SkillArchivedAfterPurchaseIsKept()
    {
        var finding = Check(currentTalents: [Pilot], currentPurchases: [Buy(Relic, Pilot)], talents: [Pilot],
            purchases: [Buy(Relic, Pilot, specifics: "Old")]);

        Assert.Equal(CharacterSkillFinding.None, finding);
    }

    [Fact]
    public void SkillWithoutMultiPurchaseCannotBeBoughtTwice()
    {
        var finding = Check(talents: [Pilot], purchases: [Buy(Flying, Pilot, count: 2)]);

        Assert.Equal(CharacterSkillProblem.NotMultiPurchase, finding.Problem);
    }

    [Fact]
    public void CountKeptAfterMultiPurchaseWasTurnedOffCanBeLowered()
    {
        var finding = Check(currentTalents: [Pilot], currentPurchases: [Buy(Flying, Pilot, count: 3)], talents: [Pilot],
            purchases: [Buy(Flying, Pilot, count: 2)]);

        Assert.Equal(CharacterSkillFinding.None, finding);
    }

    [Fact]
    public void CountKeptAfterMultiPurchaseWasTurnedOffCannotBeRaised()
    {
        var finding = Check(currentTalents: [Pilot], currentPurchases: [Buy(Flying, Pilot, count: 2)], talents: [Pilot],
            purchases: [Buy(Flying, Pilot, count: 3)]);

        Assert.Equal(CharacterSkillProblem.NotMultiPurchase, finding.Problem);
    }

    [Fact]
    public void NewSkillWithUnmetPrerequisitesIsRejected()
    {
        var finding = Check(talents: [Pilot], purchases: [Buy(Flying, Pilot), Buy(Aces, Pilot)]);

        Assert.Equivalent(new { Problem = CharacterSkillProblem.PrerequisitesNotMet, SkillIds = new[] { Aces } }, finding, strict: true);
    }

    [Fact]
    public void SkillAndItsPrerequisitesCanBeBoughtInOneSave()
    {
        var finding = Check(talents: [Pilot], purchases: [Buy(Flying, Pilot), Buy(FirstAid, Pilot), Buy(Aces, Pilot)]);

        Assert.Equal(CharacterSkillFinding.None, finding);
    }

    [Fact]
    public void OwnedSkillWhosePrerequisitesChangedIsKept()
    {
        var finding = Check(currentTalents: [Pilot], currentPurchases: [Buy(Aces, Pilot)], talents: [Pilot],
            purchases: [Buy(Aces, Pilot)]);

        Assert.Equal(CharacterSkillFinding.None, finding);
    }

    [Fact]
    public void RemovingASkillAnotherSkillNeedsIsRejectedWithTheDependentSkill()
    {
        var finding = Check(
            currentTalents: [Pilot],
            currentPurchases: [Buy(Flying, Pilot), Buy(Gunnery, Pilot), Buy(Aces, Pilot)],
            talents: [Pilot],
            purchases: [Buy(Gunnery, Pilot), Buy(Aces, Pilot)]);

        Assert.Equivalent(new { Problem = CharacterSkillProblem.RequiredByOtherSkills, SkillIds = new[] { Aces } }, finding, strict: true);
    }

    [Fact]
    public void RemovingOneChoiceOfAGroupIsAcceptedWhileAnotherChoiceIsOwned()
    {
        var finding = Check(
            currentTalents: [Pilot],
            currentPurchases: [Buy(Flying, Pilot), Buy(Gunnery, Pilot), Buy(FirstAid, Pilot), Buy(Aces, Pilot)],
            talents: [Pilot],
            purchases: [Buy(Flying, Pilot), Buy(FirstAid, Pilot), Buy(Aces, Pilot)]);

        Assert.Equal(CharacterSkillFinding.None, finding);
    }

    [Fact]
    public void RemovingASkillTogetherWithTheSkillsThatNeedItIsAccepted()
    {
        var finding = Check(
            currentTalents: [Pilot],
            currentPurchases: [Buy(Flying, Pilot), Buy(Gunnery, Pilot), Buy(Aces, Pilot)],
            talents: [Pilot],
            purchases: [Buy(Gunnery, Pilot)]);

        Assert.Equal(CharacterSkillFinding.None, finding);
    }

    [Fact]
    public void BuyingBeyondTheFreePointsIsRejected()
    {
        var finding = Check(experiencePoints: 19, talents: [Pilot], purchases: [Buy(Flying, Pilot), Buy(Gunnery, Pilot)]);

        Assert.Equivalent(new { Problem = CharacterSkillProblem.InsufficientFreePoints, SkillIds = new[] { Flying, Gunnery } }, finding, strict: true);
    }

    [Fact]
    public void SpendingEveryFreePointIsAccepted()
    {
        var finding = Check(experiencePoints: 20, talents: [Pilot], purchases: [Buy(Flying, Pilot), Buy(Gunnery, Pilot)]);

        Assert.Equal(CharacterSkillFinding.None, finding);
    }

    [Fact]
    public void RaisingAPurchaseCountBeyondTheFreePointsIsRejected()
    {
        var finding = Check(experiencePoints: 10, currentTalents: [Medic], currentPurchases: [Buy(Stamina, Medic)],
            talents: [Medic], purchases: [Buy(Stamina, Medic, count: 3)]);

        Assert.Equal(CharacterSkillProblem.InsufficientFreePoints, finding.Problem);
    }

    [Fact]
    public void NegativeFreePointsFromARaisedCostDoNotBlockOtherChanges()
    {
        var finding = Check(experiencePoints: 5, currentTalents: [Pilot], currentPurchases: [Buy(Flying, Pilot)],
            talents: [Pilot, Medic], purchases: [Buy(Flying, Pilot, specifics: "Shuttles")]);

        Assert.Equal(CharacterSkillFinding.None, finding);
    }

    [Fact]
    public void NegativeFreePointsFromARaisedCostBlockBuying()
    {
        var finding = Check(experiencePoints: 5, currentTalents: [Pilot], currentPurchases: [Buy(Flying, Pilot)],
            talents: [Pilot], purchases: [Buy(Flying, Pilot), Buy(Gunnery, Pilot)]);

        Assert.Equal(CharacterSkillProblem.InsufficientFreePoints, finding.Problem);
    }

    private static CharacterSkillFinding Check(
        IReadOnlyList<Guid> talents,
        IReadOnlyList<SkillPurchase> purchases,
        int experiencePoints = 1000,
        IReadOnlyList<Guid>? currentTalents = null,
        IReadOnlyList<SkillPurchase>? currentPurchases = null) =>
        CharacterSkillRules.Check(new CharacterSkillChange(
            experiencePoints,
            currentTalents ?? [],
            currentPurchases ?? [],
            talents,
            purchases,
            Talents,
            Skills));

    private static SkillPurchase Buy(Guid skillId, Guid talentId, int count = 1, string? specifics = null) =>
        new(skillId, talentId, count, specifics);

    private static SkillFacts Skill(
        Guid skillId,
        int cost,
        IReadOnlyList<Guid> talents,
        IReadOnlyList<IReadOnlyList<Guid>>? prerequisites = null,
        bool isMultiPurchase = false,
        bool isArchived = false) =>
        new(skillId, cost, isMultiPurchase, isArchived, talents, prerequisites ?? []);
}
