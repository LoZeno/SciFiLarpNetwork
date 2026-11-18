namespace SciFiLarpNetwork.Backend.Application.Players;

/// <param name="FreePoints">
/// Experience points minus the current cost of each owned skill, multiplied by its purchase count. It is negative
/// when a skill's cost was raised after it was bought.
/// </param>
/// <param name="TalentLimit">The most talents the character can have.</param>
/// <param name="Talents">The character's talents, in the order they were picked.</param>
/// <param name="Skills">The character's skills, in name order.</param>
/// <param name="Version">The character version; saving talents and skills changes it.</param>
public sealed record CharacterSkillsSummary(
    Guid CharacterId,
    string CharacterName,
    int ExperiencePoints,
    long FreePoints,
    int TalentLimit,
    IReadOnlyList<CharacterTalentSummary> Talents,
    IReadOnlyList<CharacterSkillPurchaseSummary> Skills,
    long Version);
