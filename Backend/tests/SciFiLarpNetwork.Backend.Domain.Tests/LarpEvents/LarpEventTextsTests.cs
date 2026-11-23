using SciFiLarpNetwork.Backend.Domain.LarpEvents;

namespace SciFiLarpNetwork.Backend.Tests.Domain.LarpEvents;

public sealed class LarpEventTextsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("A weekend at the old fort")]
    public void BlankOrShortDescriptionIsValid(string? description) =>
        Assert.True(LarpEventTexts.IsValidDescription(description));

    [Fact]
    public void DescriptionOf2000CharactersAfterTrimmingIsValid() =>
        Assert.True(LarpEventTexts.IsValidDescription($"  {new string('d', 2000)}  "));

    [Fact]
    public void DescriptionLongerThan2000CharactersIsInvalid() =>
        Assert.False(LarpEventTexts.IsValidDescription(new string('d', 2001)));

    [Fact]
    public void DescriptionWithLineBreaksAndTabsIsValid() =>
        Assert.True(LarpEventTexts.IsValidDescription("Day one:\r\n\tArrival"));

    [Fact]
    public void DescriptionWithANullCharacterIsInvalid() =>
        Assert.False(LarpEventTexts.IsValidDescription("a\0b"));

    [Theory]
    [InlineData(null, null)]
    [InlineData(" ", null)]
    [InlineData("  Fort  ", "Fort")]
    public void NormalizeTrimsAndTurnsBlankIntoNull(string? value, string? expected) =>
        Assert.Equal(expected, LarpEventTexts.Normalize(value));
}
