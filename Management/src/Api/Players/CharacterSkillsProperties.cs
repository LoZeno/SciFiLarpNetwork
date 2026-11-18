namespace SciFiLarpNetwork.Management.Api.Players;

/// <param name="FreePoints">Negative when a skill's cost was raised after it was bought.</param>
internal sealed record CharacterSkillsProperties(
    Guid CharacterId,
    string CharacterName,
    int ExperiencePoints,
    long FreePoints,
    int TalentLimit,
    IReadOnlyList<CharacterTalentProperties> Talents,
    IReadOnlyList<CharacterSkillPurchaseProperties> Skills,
    long Version);
