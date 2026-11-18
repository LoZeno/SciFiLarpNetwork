namespace SciFiLarpNetwork.Backend.Application.Players;

/// <summary>
/// The outcome of saving a character's talents and skills. <see cref="Value"/> holds the saved state when the
/// command succeeded.
/// </summary>
/// <param name="SkillNames">
/// For a failure about particular skills, their names, in name order: the skills that still need a removed skill,
/// or the requested skills that break the rule.
/// </param>
public sealed record CharacterSkillsResult(
    RosterOutcome Outcome,
    CharacterSkillsSummary? Value,
    IReadOnlyList<string> SkillNames);
