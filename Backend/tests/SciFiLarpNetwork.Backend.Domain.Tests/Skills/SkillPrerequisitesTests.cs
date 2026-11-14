using SciFiLarpNetwork.Backend.Domain.Skills;

namespace SciFiLarpNetwork.Backend.Tests.Domain.Skills;

public sealed class SkillPrerequisitesTests
{
    private static readonly Guid SkillX = Guid.NewGuid();
    private static readonly Guid SkillY = Guid.NewGuid();
    private static readonly Guid SkillZ = Guid.NewGuid();
    private static readonly Guid SkillW = Guid.NewGuid();

    [Fact]
    public void MandatoryAndChoiceGroupsHaveNoStructuralProblem()
    {
        var problem = SkillPrerequisites.FindStructuralProblem(SkillX, [[SkillY], [SkillZ, SkillW]]);

        Assert.Equal(PrerequisiteProblem.None, problem);
    }

    [Fact]
    public void EmptyGroupIsAProblem()
    {
        var problem = SkillPrerequisites.FindStructuralProblem(SkillX, [[SkillY], []]);

        Assert.Equal(PrerequisiteProblem.EmptyGroup, problem);
    }

    [Fact]
    public void SkillRequiringItselfIsAProblem()
    {
        var problem = SkillPrerequisites.FindStructuralProblem(SkillX, [[SkillY, SkillX]]);

        Assert.Equal(PrerequisiteProblem.SelfReference, problem);
    }

    [Fact]
    public void SameSkillTwiceInAGroupIsAProblem()
    {
        var problem = SkillPrerequisites.FindStructuralProblem(SkillX, [[SkillY, SkillY]]);

        Assert.Equal(PrerequisiteProblem.DuplicateInGroup, problem);
    }

    [Fact]
    public void SameSkillInTwoGroupsIsAllowed()
    {
        var problem = SkillPrerequisites.FindStructuralProblem(SkillX, [[SkillY, SkillZ], [SkillY, SkillW]]);

        Assert.Equal(PrerequisiteProblem.None, problem);
    }

    [Fact]
    public void ReferencedSkillsListsEachSkillOnce()
    {
        var referenced = SkillPrerequisites.ReferencedSkills([[SkillY, SkillZ], [SkillY]]);

        Assert.Equal(new HashSet<Guid> { SkillY, SkillZ }, referenced);
    }

    [Fact]
    public void DirectLoopIsDetected()
    {
        var existing = new Dictionary<Guid, IReadOnlyList<IReadOnlyList<Guid>>> { [SkillY] = [[SkillX]] };

        var createsLoop = SkillPrerequisites.CreatesLoop(SkillX, [[SkillY]], Lookup(existing));

        Assert.True(createsLoop);
    }

    [Fact]
    public void IndirectLoopThroughAChoiceIsDetected()
    {
        var existing = new Dictionary<Guid, IReadOnlyList<IReadOnlyList<Guid>>>
        {
            [SkillY] = [[SkillZ, SkillW]],
            [SkillW] = [[SkillX]]
        };

        var createsLoop = SkillPrerequisites.CreatesLoop(SkillX, [[SkillY]], Lookup(existing));

        Assert.True(createsLoop);
    }

    [Fact]
    public void SharedPrerequisiteWithoutLoopIsAccepted()
    {
        var existing = new Dictionary<Guid, IReadOnlyList<IReadOnlyList<Guid>>>
        {
            [SkillY] = [[SkillW]],
            [SkillZ] = [[SkillW]]
        };

        var createsLoop = SkillPrerequisites.CreatesLoop(SkillX, [[SkillY], [SkillZ]], Lookup(existing));

        Assert.False(createsLoop);
    }

    private static Func<Guid, IReadOnlyList<IReadOnlyList<Guid>>> Lookup(
        Dictionary<Guid, IReadOnlyList<IReadOnlyList<Guid>>> existing) =>
        skillId => existing.TryGetValue(skillId, out var groups) ? groups : [];
}
