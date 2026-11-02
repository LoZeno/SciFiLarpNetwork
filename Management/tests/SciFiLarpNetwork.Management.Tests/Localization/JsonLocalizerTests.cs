using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.Tests.Localization;

public sealed class JsonLocalizerTests
{
    [Fact]
    public void TranslateReturnsEnglishByDefaultAndItalianWhenRequested()
    {
        var localizer = new JsonLocalizer();

        Assert.Equal("Connect", localizer.Translate("management.connect", "en"));
        Assert.Equal("Connetti", localizer.Translate("management.connect", "it"));
    }

    [Fact]
    public void TranslateUsesEnglishForUnsupportedLocale()
    {
        var localizer = new JsonLocalizer();

        Assert.Equal("Connect", localizer.Translate("management.connect", "fr"));
    }

    [Fact]
    public void TranslateUsesGenericTextForUnknownKey()
    {
        var localizer = new JsonLocalizer();

        Assert.Equal("An unexpected error occurred.", localizer.Translate("missing.key", "en"));
    }

    [Fact]
    public void TranslateUsesEnglishFallbackForMissingItalianText()
    {
        var localizer = new JsonLocalizer();

        Assert.Equal("An unexpected error occurred.", localizer.Translate("missing.key", "it"));
    }
}
