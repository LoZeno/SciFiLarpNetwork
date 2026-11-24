using SciFiLarpNetwork.Backend.Domain.Texts;

namespace SciFiLarpNetwork.Backend.Tests.Domain.Texts;

public sealed class FreeTextTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Line one\r\nLine two\n\tIndented")]
    public void MissingTextOrTextWithLineBreaksAndTabsIsStorable(string? text) =>
        Assert.True(FreeText.IsStorable(text));

    [Fact]
    public void TextWithANullCharacterIsNotStorable() =>
        Assert.False(FreeText.IsStorable("a\0b"));
}
