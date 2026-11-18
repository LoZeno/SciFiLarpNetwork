namespace SciFiLarpNetwork.Backend.Application.Players;

public enum RosterOutcome
{
    Succeeded,
    NotFound,
    StaleVersion,

    /// <summary>
    /// Another command changed shared data first (the player, a running-number sequence, or another character of
    /// the player), but not the edited item itself; nothing was saved, and saving again re-checks every rule.
    /// </summary>
    ConcurrentChange,

    InvalidName,
    InvalidEmail,
    InvalidPhone,
    InvalidNotes,
    InvalidPlayerType,
    InvalidCharacterText,
    InvalidExperiencePoints,
    InvalidCharacterStatus,
    DeathDateRequired,

    /// <summary>The player is Inactive, so the character cannot be created or become Active.</summary>
    PlayerInactive,

    /// <summary>Only Inactive players can be archived.</summary>
    PlayerNotInactive,

    /// <summary>Only Retired or Dead characters can be archived.</summary>
    CharacterNotArchivable,

    /// <summary>An archived player or character must be unarchived before it can become Active.</summary>
    Archived,

    UnknownNetwork,
    NetworkDisabled,
    DefaultNetworkNotSelectable,
    UnknownStat,
    InvalidStatValue,
    InvalidStatName,
    InvalidStatType,
    NameAlreadyUsed,

    /// <summary>A typed stat name belongs to an archived stat, which must be renamed or unarchived first.</summary>
    StatArchived,

    InvalidQrIdentifier,

    /// <summary>A character can have at most five talents.</summary>
    TooManyTalents,

    DuplicateTalent,

    DuplicateSkillPurchase,
    InvalidPurchaseCount,
    InvalidSpecifics,
    UnknownTalent,

    /// <summary>A newly picked talent is archived.</summary>
    TalentArchived,

    UnknownSkill,

    /// <summary>A skill is bought through a talent the character does not have.</summary>
    TalentNotOwned,

    /// <summary>A skill is bought, or moved, through a talent it does not belong to.</summary>
    SkillNotInTalent,

    /// <summary>An archived skill is bought, or its purchase count raised.</summary>
    SkillArchived,

    /// <summary>A skill without the multi-purchase flag gets a purchase count above 1.</summary>
    NotMultiPurchase,

    PrerequisitesNotMet,

    /// <summary>A removed skill is still needed by another owned skill.</summary>
    RequiredByOtherSkills,

    /// <summary>
    /// Free points would be negative after buying a skill, raising a purchase count, or lowering experience points.
    /// </summary>
    InsufficientFreePoints
}
