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
        "errors.statArchived"
    ];

    private static readonly string[] Locales = ["en", "it"];

    public static TheoryData<string, string> RosterErrorsInEveryLocale =>
        new(RosterErrorCodes.SelectMany(code => Locales.Select(locale => (code, locale))));

    [Theory]
    [MemberData(nameof(RosterErrorsInEveryLocale))]
    public void RosterErrorShowsItsOwnMessage(string code, string locale)
    {
        var localizer = new JsonLocalizer();

        Assert.DoesNotContain(localizer.TranslateApiError(code, locale), (string[])[localizer.Translate("errors.requestFailed", locale), code]);
    }
}
