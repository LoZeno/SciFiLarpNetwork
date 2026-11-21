namespace SciFiLarpNetwork.Backend.Application.LarpEvents;

public enum LarpEventOutcome
{
    Succeeded,
    NotFound,
    StaleVersion,

    /// <summary>
    /// Another command changed the event, the character, or a running-number sequence first; nothing was saved, and
    /// trying again re-checks every rule.
    /// </summary>
    ConcurrentChange,

    InvalidName,
    InvalidDescription,

    /// <summary>A base or day experience value must be blank or a whole number of 0 or more.</summary>
    InvalidExperienceValue,

    /// <summary>
    /// A closing award names a character that is not registered, a day the character is not registered for or that has
    /// no experience, base experience the event does not have, or experience an earlier close already gave.
    /// </summary>
    AwardNotAllowed,

    /// <summary>The event is Closed, so its details, days, registrations, and experience cannot change.</summary>
    EventClosed,

    /// <summary>Only Closed events can be archived.</summary>
    EventNotClosed,

    /// <summary>An archived event must be unarchived before it can be reopened.</summary>
    Archived,

    /// <summary>The event already has a day on that date.</summary>
    DuplicateDate,

    UnknownDay,

    /// <summary>A day with registered characters cannot be removed.</summary>
    DayHasParticipants,

    NoDaysSelected,
    UnknownCharacter,

    /// <summary>Only Active characters can be registered.</summary>
    CharacterNotActive,

    /// <summary>The character is already registered for every requested day.</summary>
    AlreadyRegistered,

    /// <summary>The character is not registered for the event, or for a requested day.</summary>
    NotRegistered,

    /// <summary>An experience change must not be zero.</summary>
    InvalidExperienceChange,

    /// <summary>The change would take the character's experience points below 0 or above the largest whole number.</summary>
    ExperienceOutOfRange,

    /// <summary>The change would make the character's free points negative.</summary>
    InsufficientFreePoints,

    /// <summary>No character is registered for the event or the selected day, so there is nothing to print.</summary>
    NoParticipants,

    /// <summary>More characters are registered than one print request can hold.</summary>
    TooManyCards
}
