namespace SciFiLarpNetwork.Backend.Application.Skills;

public enum CatalogOutcome
{
    Succeeded,
    NotFound,
    StaleVersion,
    InvalidName,
    NameAlreadyUsed,
    InvalidCost,
    UnknownTalent,
    TalentArchived,
    UnknownCardFlag,
    InvalidCardFlagName,
    EmptyPrerequisiteGroup,
    SelfPrerequisite,
    DuplicatePrerequisite,
    UnknownPrerequisite,
    PrerequisiteLoop,

    /// <summary>
    /// Another command changed shared data (the prerequisite graph or an assigned talent) first, but not the edited
    /// skill itself; nothing was saved, and saving again re-checks every rule.
    /// </summary>
    ConcurrentChange
}
