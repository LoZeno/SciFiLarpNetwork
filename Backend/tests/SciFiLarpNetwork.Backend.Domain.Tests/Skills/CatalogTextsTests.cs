using SciFiLarpNetwork.Backend.Domain.Skills;

namespace SciFiLarpNetwork.Backend.Tests.Domain.Skills;

public sealed class CatalogTextsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("Healing arts:\r\n\tfield surgery")]
    public void BlankOrMultiLineDescriptionIsValid(string? description) =>
        Assert.True(CatalogTexts.IsValidDescription(description));

    [Fact]
    public void DescriptionOf2000CharactersAfterTrimmingIsValid() =>
        Assert.True(CatalogTexts.IsValidDescription($"  {new string('d', 2000)}  "));

    [Fact]
    public void DescriptionLongerThan2000CharactersIsInvalid() =>
        Assert.False(CatalogTexts.IsValidDescription(new string('d', 2001)));

    [Fact]
    public void DescriptionWithANullCharacterIsInvalid() =>
        Assert.False(CatalogTexts.IsValidDescription("a\0b"));
}
