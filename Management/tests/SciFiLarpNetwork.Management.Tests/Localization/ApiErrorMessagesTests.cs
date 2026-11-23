using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.Tests.Localization;

public sealed class ApiErrorMessagesTests
{
    private static readonly string[] RosterErrorCodes =
    [
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
        "errors.noCharactersSelected",
        "errors.tooManyCharacters",
        "errors.unknownCharacters",
        "errors.cardRenderingFailed",
        "errors.invalidCardTemplate",
        "errors.cardTemplateTooLarge",
        "errors.cardTemplateNotSaved"
    ];

    private static readonly string[] EventErrorCodes =
    [
        "errors.invalidDescription",
        "errors.invalidExperienceValue",
        "errors.awardNotAllowed",
        "errors.eventClosed",
        "errors.eventNotClosed",
        "errors.duplicateDate",
        "errors.unknownDay",
        "errors.dayHasParticipants",
        "errors.dayExperienceGiven",
        "errors.participantHasExperience",
        "errors.noDaysSelected",
        "errors.unknownCharacter",
        "errors.characterNotActive",
        "errors.alreadyRegistered",
        "errors.notRegistered",
        "errors.invalidExperienceChange",
        "errors.experienceOutOfRange",
        "errors.noParticipants"
    ];

    private static readonly string[] MessageErrorCodes =
    [
        "errors.invalidMessageId",
        "errors.invalidMessageTitle",
        "errors.invalidMessageBody",
        "errors.noRecipients",
        "errors.tooManyRecipients",
        "errors.archivedCharacters",
        "errors.unknownReplySource",
        "errors.messageIdInUse"
    ];

    private static readonly string[] Locales = ["en", "it"];

    public static TheoryData<string, string> RosterErrorsInEveryLocale =>
        new(RosterErrorCodes.SelectMany(code => Locales.Select(locale => (code, locale))));

    public static TheoryData<string, string> EventErrorsInEveryLocale =>
        new(EventErrorCodes.SelectMany(code => Locales.Select(locale => (code, locale))));

    public static TheoryData<string, string> MessageErrorsInEveryLocale =>
        new(MessageErrorCodes.SelectMany(code => Locales.Select(locale => (code, locale))));

    [Theory]
    [MemberData(nameof(RosterErrorsInEveryLocale))]
    public void RosterErrorShowsItsOwnMessage(string code, string locale)
    {
        var localizer = new JsonLocalizer();

        Assert.DoesNotContain(localizer.TranslateApiError(code, locale), (string[])[localizer.Translate("errors.requestFailed", locale), code]);
    }

    [Theory]
    [MemberData(nameof(EventErrorsInEveryLocale))]
    public void EventErrorShowsItsOwnMessage(string code, string locale)
    {
        var localizer = new JsonLocalizer();

        Assert.DoesNotContain(localizer.TranslateApiError(code, locale), (string[])[localizer.Translate("errors.requestFailed", locale), code]);
    }

    [Theory]
    [MemberData(nameof(MessageErrorsInEveryLocale))]
    public void MessageErrorShowsItsOwnMessage(string code, string locale)
    {
        var localizer = new JsonLocalizer();

        Assert.DoesNotContain(localizer.TranslateApiError(code, locale), (string[])[localizer.Translate("errors.requestFailed", locale), code]);
    }
}
