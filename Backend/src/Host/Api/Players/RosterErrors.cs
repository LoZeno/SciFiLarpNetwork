using SciFiLarpNetwork.Backend.Api.Skills;
using SciFiLarpNetwork.Backend.Application.Players;

namespace SciFiLarpNetwork.Backend.Api.Players;

/// <summary>
/// Maps player, character, and stat command outcomes to stable Siren error codes. Each code is also its
/// localization key.
/// </summary>
public static class RosterErrors
{
    public const string InvalidEmail = "errors.invalidEmail";
    public const string InvalidPhone = "errors.invalidPhone";
    public const string InvalidNotes = "errors.invalidNotes";
    public const string InvalidPlayerType = "errors.invalidPlayerType";
    public const string InvalidCharacterText = "errors.invalidCharacterText";
    public const string InvalidExperiencePoints = "errors.invalidExperiencePoints";
    public const string InvalidCharacterStatus = "errors.invalidCharacterStatus";
    public const string DeathDateRequired = "errors.deathDateRequired";
    public const string PlayerInactive = "errors.playerInactive";
    public const string PlayerNotInactive = "errors.playerNotInactive";
    public const string CharacterNotArchivable = "errors.characterNotArchivable";
    public const string Archived = "errors.archived";
    public const string UnknownNetwork = "errors.unknownNetwork";
    public const string DefaultNetworkNotSelectable = "errors.defaultNetworkNotSelectable";
    public const string DisabledNetworkNotSelectable = "errors.disabledNetworkNotSelectable";
    public const string UnknownStat = "errors.unknownStat";
    public const string InvalidStatValue = "errors.invalidStatValue";
    public const string InvalidStatName = "errors.invalidStatName";
    public const string InvalidStatType = "errors.invalidStatType";
    public const string InvalidQrIdentifier = "errors.invalidQrIdentifier";
    public const string StatArchived = "errors.statArchived";
    public const string TooManyTalents = "errors.tooManyTalents";
    public const string DuplicateSkillPurchase = "errors.duplicateSkillPurchase";
    public const string InvalidPurchaseCount = "errors.invalidPurchaseCount";
    public const string InvalidSpecifics = "errors.invalidSpecifics";
    public const string ArchivedTalentNotSelectable = "errors.archivedTalentNotSelectable";
    public const string UnknownSkill = "errors.unknownSkill";
    public const string TalentNotOwned = "errors.talentNotOwned";
    public const string SkillNotInTalent = "errors.skillNotInTalent";
    public const string SkillArchived = "errors.skillArchived";
    public const string NotMultiPurchase = "errors.notMultiPurchase";
    public const string PrerequisitesNotMet = "errors.prerequisitesNotMet";
    public const string RequiredByOtherSkills = "errors.requiredByOtherSkills";
    public const string InsufficientFreePoints = "errors.insufficientFreePoints";

    /// <summary>The error detail listing the names of the skills a character skill error is about.</summary>
    public const string SkillsErrorKey = "skills";

    public static ApiException ToApiException(RosterOutcome outcome) =>
        outcome switch
        {
            RosterOutcome.NotFound => Create(StatusCodes.Status404NotFound, ApiErrorCodes.NotFound),
            RosterOutcome.StaleVersion => Create(StatusCodes.Status409Conflict, CatalogErrors.StaleVersion),
            RosterOutcome.ConcurrentChange => Create(StatusCodes.Status409Conflict, CatalogErrors.ConcurrentChange),
            RosterOutcome.NameAlreadyUsed => Create(StatusCodes.Status409Conflict, CatalogErrors.NameAlreadyUsed),
            RosterOutcome.PlayerInactive => Create(StatusCodes.Status409Conflict, PlayerInactive),
            RosterOutcome.PlayerNotInactive => Create(StatusCodes.Status409Conflict, PlayerNotInactive),
            RosterOutcome.CharacterNotArchivable => Create(StatusCodes.Status409Conflict, CharacterNotArchivable),
            RosterOutcome.Archived => Create(StatusCodes.Status409Conflict, Archived),
            RosterOutcome.StatArchived => Create(StatusCodes.Status409Conflict, StatArchived),
            RosterOutcome.InvalidName => Create(StatusCodes.Status400BadRequest, CatalogErrors.InvalidName),
            RosterOutcome.InvalidEmail => Create(StatusCodes.Status400BadRequest, InvalidEmail),
            RosterOutcome.InvalidPhone => Create(StatusCodes.Status400BadRequest, InvalidPhone),
            RosterOutcome.InvalidNotes => Create(StatusCodes.Status400BadRequest, InvalidNotes),
            RosterOutcome.InvalidPlayerType => Create(StatusCodes.Status400BadRequest, InvalidPlayerType),
            RosterOutcome.InvalidCharacterText => Create(StatusCodes.Status400BadRequest, InvalidCharacterText),
            RosterOutcome.InvalidExperiencePoints => Create(StatusCodes.Status400BadRequest, InvalidExperiencePoints),
            RosterOutcome.InvalidCharacterStatus => Create(StatusCodes.Status400BadRequest, InvalidCharacterStatus),
            RosterOutcome.DeathDateRequired => Create(StatusCodes.Status400BadRequest, DeathDateRequired),
            RosterOutcome.UnknownNetwork => Create(StatusCodes.Status400BadRequest, UnknownNetwork),
            RosterOutcome.NetworkDisabled => Create(StatusCodes.Status400BadRequest, DisabledNetworkNotSelectable),
            RosterOutcome.DefaultNetworkNotSelectable => Create(StatusCodes.Status400BadRequest, DefaultNetworkNotSelectable),
            RosterOutcome.UnknownStat => Create(StatusCodes.Status400BadRequest, UnknownStat),
            RosterOutcome.InvalidStatValue => Create(StatusCodes.Status400BadRequest, InvalidStatValue),
            RosterOutcome.InvalidStatName => Create(StatusCodes.Status400BadRequest, InvalidStatName),
            RosterOutcome.InvalidStatType => Create(StatusCodes.Status400BadRequest, InvalidStatType),
            RosterOutcome.InvalidQrIdentifier => Create(StatusCodes.Status400BadRequest, InvalidQrIdentifier),
            RosterOutcome.TooManyTalents => Create(StatusCodes.Status400BadRequest, TooManyTalents),
            RosterOutcome.DuplicateSkillPurchase => Create(StatusCodes.Status400BadRequest, DuplicateSkillPurchase),
            RosterOutcome.InvalidPurchaseCount => Create(StatusCodes.Status400BadRequest, InvalidPurchaseCount),
            RosterOutcome.InvalidSpecifics => Create(StatusCodes.Status400BadRequest, InvalidSpecifics),
            RosterOutcome.UnknownTalent => Create(StatusCodes.Status400BadRequest, CatalogErrors.UnknownTalent),
            RosterOutcome.UnknownSkill => Create(StatusCodes.Status400BadRequest, UnknownSkill),
            RosterOutcome.TalentNotOwned => Create(StatusCodes.Status400BadRequest, TalentNotOwned),
            RosterOutcome.TalentArchived => Create(StatusCodes.Status409Conflict, ArchivedTalentNotSelectable),
            RosterOutcome.SkillNotInTalent => Create(StatusCodes.Status409Conflict, SkillNotInTalent),
            RosterOutcome.SkillArchived => Create(StatusCodes.Status409Conflict, SkillArchived),
            RosterOutcome.NotMultiPurchase => Create(StatusCodes.Status409Conflict, NotMultiPurchase),
            RosterOutcome.PrerequisitesNotMet => Create(StatusCodes.Status409Conflict, PrerequisitesNotMet),
            RosterOutcome.RequiredByOtherSkills => Create(StatusCodes.Status409Conflict, RequiredByOtherSkills),
            RosterOutcome.InsufficientFreePoints => Create(StatusCodes.Status409Conflict, InsufficientFreePoints),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "A successful outcome is not an error.")
        };

    public static T Unwrap<T>(RosterResult<T> result)
        where T : class =>
        result.Outcome == RosterOutcome.Succeeded && result.Value is { } value
            ? value
            : throw ToApiException(result.Outcome);

    /// <summary>
    /// Returns the saved talents and skills, or throws the error; an error about particular skills lists their names
    /// under <see cref="SkillsErrorKey"/>.
    /// </summary>
    public static CharacterSkillsSummary Unwrap(CharacterSkillsResult result)
    {
        if (result.Outcome == RosterOutcome.Succeeded && result.Value is { } value)
        {
            return value;
        }

        var error = ToApiException(result.Outcome);
        throw result.SkillNames.Count == 0
            ? error
            : new ApiException(error.StatusCode, error.Code, error.MessageKey,
                new Dictionary<string, IReadOnlyList<string>> { [SkillsErrorKey] = result.SkillNames });
    }

    private static ApiException Create(int statusCode, string code) => new(statusCode, code, code);
}
