using System.Net;

namespace SciFiLarpNetwork.Management.Localization;

/// <summary>
/// Turns API error codes into localized text. Only known codes are used as keys, so a server cannot
/// choose arbitrary resource text; anything else falls back to a status-based message.
/// </summary>
public static class ApiErrorMessages
{
    private static readonly HashSet<string> KnownCodes =
    [
        "errors.validation",
        "errors.unauthorized",
        "errors.forbidden",
        "errors.notFound",
        "errors.conflict",
        "errors.requestFailed",
        "errors.serviceUnavailable",
        "errors.internalServerError",
        "errors.staleVersion",
        "errors.nameAlreadyUsed",
        "errors.invalidName",
        "errors.invalidCost",
        "errors.unknownTalent",
        "errors.talentArchived",
        "errors.unknownCardFlag",
        "errors.invalidCardFlagName",
        "errors.emptyPrerequisiteGroup",
        "errors.selfPrerequisite",
        "errors.duplicatePrerequisite",
        "errors.unknownPrerequisite",
        "errors.prerequisiteLoop",
        "errors.concurrentChange",
        "errors.codeAlreadyUsed",
        "errors.invalidCode",
        "errors.invalidTheme",
        "errors.invalidImage",
        "errors.imageTooLarge",
        "errors.imageDimensionsTooLarge",
        "errors.networkDisabled",
        "errors.defaultNetworkCannotBeDisabled",
        "errors.invalidEmail",
        "errors.invalidPhone",
        "errors.invalidNotes",
        "errors.invalidPlayerType",
        "errors.invalidCharacterText",
        "errors.invalidExperiencePoints",
        "errors.invalidCharacterStatus",
        "errors.deathDateRequired",
        "errors.playerInactive",
        "errors.playerNotInactive",
        "errors.characterNotArchivable",
        "errors.archived",
        "errors.unknownNetwork",
        "errors.defaultNetworkNotSelectable",
        "errors.disabledNetworkNotSelectable",
        "errors.unknownStat",
        "errors.invalidStatValue",
        "errors.invalidStatName",
        "errors.invalidStatType",
        "errors.invalidQrIdentifier",
        "errors.statArchived",
        "errors.tooManyTalents",
        "errors.duplicateTalent",
        "errors.duplicateSkillPurchase",
        "errors.invalidPurchaseCount",
        "errors.invalidSpecifics",
        "errors.archivedTalentNotSelectable",
        "errors.unknownSkill",
        "errors.talentNotOwned",
        "errors.skillNotInTalent",
        "errors.skillArchived",
        "errors.notMultiPurchase",
        "errors.prerequisitesNotMet",
        "errors.requiredByOtherSkills",
        "errors.insufficientFreePoints",
        "errors.noCharactersSelected",
        "errors.tooManyCharacters",
        "errors.unknownCharacters",
        "errors.cardRenderingFailed"
    ];

    extension(IManagementLocalizer localizer)
    {
        public string TranslateApiError(string errorCode, string locale) =>
            localizer.Translate(ResolveKey(errorCode), locale);
    }

    private static string ResolveKey(string errorCode) =>
        KnownCodes.Contains(errorCode)
            ? errorCode
            : TryGetStatusCode(errorCode, out var statusCode)
                ? statusCode switch
                {
                    HttpStatusCode.Unauthorized => "errors.unauthorized",
                    HttpStatusCode.Forbidden => "errors.forbidden",
                    HttpStatusCode.NotFound => "errors.notFound",
                    HttpStatusCode.Conflict => "errors.conflict",
                    HttpStatusCode.ServiceUnavailable => "errors.serviceUnavailable",
                    >= HttpStatusCode.InternalServerError => "errors.internalServerError",
                    _ => "errors.requestFailed"
                }
                : "errors.requestFailed";

    private static bool TryGetStatusCode(string errorCode, out HttpStatusCode statusCode)
    {
        statusCode = default;
        if (!errorCode.StartsWith("http.", StringComparison.Ordinal) ||
            !int.TryParse(errorCode.AsSpan("http.".Length), out var numericStatus))
        {
            return false;
        }

        var parsedStatus = (HttpStatusCode)numericStatus;
        if (!Enum.IsDefined(parsedStatus))
        {
            return false;
        }

        statusCode = parsedStatus;
        return true;
    }
}
