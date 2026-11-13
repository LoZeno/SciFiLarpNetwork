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
    PrerequisiteLoop
}
