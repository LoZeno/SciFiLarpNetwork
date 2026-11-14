namespace SciFiLarpNetwork.Backend.Domain.Skills;

/// <summary>
/// Rules for a skill's prerequisites. Prerequisites are requirement groups: a character must meet every
/// group, and meets a group by owning at least one of its skills.
/// </summary>
public static class SkillPrerequisites
{
    public static PrerequisiteProblem FindStructuralProblem(
        Guid skillId,
        IReadOnlyList<IReadOnlyList<Guid>> groups)
    {
        foreach (var group in groups)
        {
            if (group.Count == 0)
            {
                return PrerequisiteProblem.EmptyGroup;
            }

            if (group.Contains(skillId))
            {
                return PrerequisiteProblem.SelfReference;
            }

            if (group.Distinct().Count() != group.Count)
            {
                return PrerequisiteProblem.DuplicateInGroup;
            }
        }

        return PrerequisiteProblem.None;
    }

    public static IReadOnlySet<Guid> ReferencedSkills(IReadOnlyList<IReadOnlyList<Guid>> groups) =>
        groups.SelectMany(group => group).ToHashSet();

    /// <summary>
    /// Reports whether giving <paramref name="skillId"/> the <paramref name="groups"/> would make it require
    /// itself through any chain of prerequisites. Every skill in every group is followed, including choices.
    /// </summary>
    /// <param name="prerequisitesOf">The current prerequisite groups of other skills; unknown skills have none.</param>
    public static bool CreatesLoop(
        Guid skillId,
        IReadOnlyList<IReadOnlyList<Guid>> groups,
        Func<Guid, IReadOnlyList<IReadOnlyList<Guid>>> prerequisitesOf)
    {
        var visited = new HashSet<Guid>();
        var pending = new Queue<Guid>(ReferencedSkills(groups));
        while (pending.TryDequeue(out var current))
        {
            if (current == skillId)
            {
                return true;
            }

            if (!visited.Add(current))
            {
                continue;
            }

            foreach (var next in ReferencedSkills(prerequisitesOf(current)))
            {
                pending.Enqueue(next);
            }
        }

        return false;
    }
}
