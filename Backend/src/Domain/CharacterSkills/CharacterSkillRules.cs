namespace SciFiLarpNetwork.Backend.Domain.CharacterSkills;

/// <summary>
/// Rules for a character's talents and skills. Rules about buying are checked only for skills that are bought or
/// whose purchase count is raised; later catalog changes never invalidate a purchase that is kept as it is.
/// </summary>
public static class CharacterSkillRules
{
    public const int MaxTalents = 5;

    public const int MaxPurchaseCount = 999;

    /// <summary>Experience points minus the current cost of each owned skill, multiplied by its purchase count.</summary>
    public static long FreePoints(int experiencePoints, IEnumerable<SkillPurchase> purchases, Func<Guid, int> costOf) =>
        experiencePoints - purchases.Sum(purchase => (long)costOf(purchase.SkillId) * purchase.Count);

    /// <summary>
    /// Finds the first broken rule, in this order: talents, skill and talent references, archived skills,
    /// multi-purchase counts, prerequisites, skills still needed by others, and free points.
    /// Structural rules (the talent limit, duplicates, counts, specifics) are checked before this.
    /// </summary>
    public static CharacterSkillFinding Check(CharacterSkillChange change)
    {
        var current = change.CurrentPurchases.ToDictionary(purchase => purchase.SkillId);
        if (FindTalentProblem(change) is { } talentProblem)
        {
            return new CharacterSkillFinding(talentProblem, []);
        }

        // Later checks look skills up in the catalog, so they run only after every skill is known to exist.
        Func<CharacterSkillFinding>[] checks =
        [
            () => Find(CharacterSkillProblem.UnknownSkill, change, current, (purchase, _) =>
                !change.Skills.ContainsKey(purchase.SkillId)),
            () => Find(CharacterSkillProblem.TalentNotOwned, change, current, (purchase, _) =>
                !change.TalentIds.Contains(purchase.TalentId)),
            () => Find(CharacterSkillProblem.SkillNotInTalent, change, current, (purchase, previous) =>
                (previous?.TalentId != purchase.TalentId || IsRaised(purchase, previous)) &&
                !change.Skills[purchase.SkillId].TalentIds.Contains(purchase.TalentId)),
            () => Find(CharacterSkillProblem.SkillArchived, change, current, (purchase, previous) =>
                IsRaised(purchase, previous) && change.Skills[purchase.SkillId].IsArchived),
            () => Find(CharacterSkillProblem.NotMultiPurchase, change, current, (purchase, previous) =>
                IsRaised(purchase, previous) && purchase.Count > 1 && !change.Skills[purchase.SkillId].IsMultiPurchase),
            () => FindUnmetPrerequisites(change, current),
            () => FindDependentSkills(change, current),
            () => FindInsufficientFreePoints(change, current)
        ];
        return checks.Select(check => check()).FirstOrDefault(finding => finding.Problem != CharacterSkillProblem.None)
               ?? CharacterSkillFinding.None;
    }

    private static CharacterSkillProblem? FindTalentProblem(CharacterSkillChange change)
    {
        foreach (var talentId in change.TalentIds.Except(change.CurrentTalentIds))
        {
            if (!change.Talents.TryGetValue(talentId, out var talent))
            {
                return CharacterSkillProblem.UnknownTalent;
            }

            if (talent.IsArchived)
            {
                return CharacterSkillProblem.TalentArchived;
            }
        }

        return null;
    }

    private static CharacterSkillFinding Find(
        CharacterSkillProblem problem,
        CharacterSkillChange change,
        Dictionary<Guid, SkillPurchase> current,
        Func<SkillPurchase, SkillPurchase?, bool> breaksRule)
    {
        var skillIds = change.Purchases
            .Where(purchase => breaksRule(purchase, current.GetValueOrDefault(purchase.SkillId)))
            .Select(purchase => purchase.SkillId)
            .ToArray();
        return skillIds.Length == 0 ? CharacterSkillFinding.None : new CharacterSkillFinding(problem, skillIds);
    }

    private static bool IsRaised(SkillPurchase purchase, SkillPurchase? previous) =>
        purchase.Count > (previous?.Count ?? 0);

    private static CharacterSkillFinding FindUnmetPrerequisites(
        CharacterSkillChange change,
        Dictionary<Guid, SkillPurchase> current)
    {
        var owned = change.Purchases.Select(purchase => purchase.SkillId).ToHashSet();
        var unmet = change.Purchases
            .Where(purchase => IsRaised(purchase, current.GetValueOrDefault(purchase.SkillId)))
            .Where(purchase => !change.Skills[purchase.SkillId].PrerequisiteGroups.All(group => group.Any(owned.Contains)))
            .Select(purchase => purchase.SkillId)
            .ToArray();
        return unmet.Length == 0
            ? CharacterSkillFinding.None
            : new CharacterSkillFinding(CharacterSkillProblem.PrerequisitesNotMet, unmet);
    }

    /// <summary>
    /// A kept skill depends on a removed skill when one of its prerequisite groups lists the removed skill and no
    /// longer has any owned skill.
    /// </summary>
    private static CharacterSkillFinding FindDependentSkills(
        CharacterSkillChange change,
        Dictionary<Guid, SkillPurchase> current)
    {
        var owned = change.Purchases.Select(purchase => purchase.SkillId).ToHashSet();
        var removed = current.Keys.Where(skillId => !owned.Contains(skillId)).ToHashSet();
        var dependents = change.Purchases
            .Where(purchase => current.ContainsKey(purchase.SkillId))
            .Where(purchase => change.Skills[purchase.SkillId].PrerequisiteGroups
                .Any(group => group.Any(removed.Contains) && !group.Any(owned.Contains)))
            .Select(purchase => purchase.SkillId)
            .ToArray();
        return dependents.Length == 0
            ? CharacterSkillFinding.None
            : new CharacterSkillFinding(CharacterSkillProblem.RequiredByOtherSkills, dependents);
    }

    /// <summary>
    /// Free points may already be negative after a skill's cost was raised; that only blocks buying more.
    /// </summary>
    private static CharacterSkillFinding FindInsufficientFreePoints(
        CharacterSkillChange change,
        Dictionary<Guid, SkillPurchase> current)
    {
        var raised = change.Purchases
            .Where(purchase => IsRaised(purchase, current.GetValueOrDefault(purchase.SkillId)))
            .Select(purchase => purchase.SkillId)
            .ToArray();
        return raised.Length > 0 &&
               FreePoints(change.ExperiencePoints, change.Purchases, skillId => change.Skills[skillId].Cost) < 0
            ? new CharacterSkillFinding(CharacterSkillProblem.InsufficientFreePoints, raised)
            : CharacterSkillFinding.None;
    }
}
