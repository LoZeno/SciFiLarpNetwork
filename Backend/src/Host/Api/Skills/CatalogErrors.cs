using SciFiLarpNetwork.Backend.Application.Skills;

namespace SciFiLarpNetwork.Backend.Api.Skills;

/// <summary>
/// Maps catalog command outcomes to stable Siren error codes. Each code is also its localization key.
/// </summary>
public static class CatalogErrors
{
    public const string StaleVersion = "errors.staleVersion";
    public const string NameAlreadyUsed = "errors.nameAlreadyUsed";
    public const string InvalidName = "errors.invalidName";
    public const string InvalidCost = "errors.invalidCost";
    public const string UnknownTalent = "errors.unknownTalent";
    public const string TalentArchived = "errors.talentArchived";
    public const string UnknownCardFlag = "errors.unknownCardFlag";
    public const string InvalidCardFlagName = "errors.invalidCardFlagName";
    public const string EmptyPrerequisiteGroup = "errors.emptyPrerequisiteGroup";
    public const string SelfPrerequisite = "errors.selfPrerequisite";
    public const string DuplicatePrerequisite = "errors.duplicatePrerequisite";
    public const string UnknownPrerequisite = "errors.unknownPrerequisite";
    public const string PrerequisiteLoop = "errors.prerequisiteLoop";

    public static ApiException ToApiException(CatalogOutcome outcome) =>
        outcome switch
        {
            CatalogOutcome.NotFound => Create(StatusCodes.Status404NotFound, ApiErrorCodes.NotFound),
            CatalogOutcome.StaleVersion => Create(StatusCodes.Status409Conflict, StaleVersion),
            CatalogOutcome.NameAlreadyUsed => Create(StatusCodes.Status409Conflict, NameAlreadyUsed),
            CatalogOutcome.InvalidName => Create(StatusCodes.Status400BadRequest, InvalidName),
            CatalogOutcome.InvalidCost => Create(StatusCodes.Status400BadRequest, InvalidCost),
            CatalogOutcome.UnknownTalent => Create(StatusCodes.Status400BadRequest, UnknownTalent),
            CatalogOutcome.TalentArchived => Create(StatusCodes.Status400BadRequest, TalentArchived),
            CatalogOutcome.UnknownCardFlag => Create(StatusCodes.Status400BadRequest, UnknownCardFlag),
            CatalogOutcome.InvalidCardFlagName => Create(StatusCodes.Status400BadRequest, InvalidCardFlagName),
            CatalogOutcome.EmptyPrerequisiteGroup => Create(StatusCodes.Status400BadRequest, EmptyPrerequisiteGroup),
            CatalogOutcome.SelfPrerequisite => Create(StatusCodes.Status400BadRequest, SelfPrerequisite),
            CatalogOutcome.DuplicatePrerequisite => Create(StatusCodes.Status400BadRequest, DuplicatePrerequisite),
            CatalogOutcome.UnknownPrerequisite => Create(StatusCodes.Status400BadRequest, UnknownPrerequisite),
            CatalogOutcome.PrerequisiteLoop => Create(StatusCodes.Status400BadRequest, PrerequisiteLoop),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "A successful outcome is not an error.")
        };

    public static T Unwrap<T>(CatalogResult<T> result)
        where T : class =>
        result.Outcome == CatalogOutcome.Succeeded && result.Value is { } value
            ? value
            : throw ToApiException(result.Outcome);

    private static ApiException Create(int statusCode, string code) => new(statusCode, code, code);
}
