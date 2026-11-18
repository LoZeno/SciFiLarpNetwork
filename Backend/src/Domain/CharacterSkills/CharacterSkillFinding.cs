namespace SciFiLarpNetwork.Backend.Domain.CharacterSkills;

/// <param name="SkillIds">
/// The skills the problem is about: for <see cref="CharacterSkillProblem.RequiredByOtherSkills"/>, the owned skills
/// that still need a removed skill; otherwise the requested skills that break the rule.
/// </param>
public sealed record CharacterSkillFinding(CharacterSkillProblem Problem, IReadOnlyList<Guid> SkillIds)
{
    public static CharacterSkillFinding None { get; } = new(CharacterSkillProblem.None, []);
}
