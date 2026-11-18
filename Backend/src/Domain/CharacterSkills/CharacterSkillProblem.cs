namespace SciFiLarpNetwork.Backend.Domain.CharacterSkills;

public enum CharacterSkillProblem
{
    None,
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

    /// <summary>A newly bought skill's prerequisites are not met.</summary>
    PrerequisitesNotMet,

    /// <summary>A removed skill is still needed by another owned skill.</summary>
    RequiredByOtherSkills,

    /// <summary>Free points would be negative after buying a skill or raising a purchase count.</summary>
    InsufficientFreePoints
}
