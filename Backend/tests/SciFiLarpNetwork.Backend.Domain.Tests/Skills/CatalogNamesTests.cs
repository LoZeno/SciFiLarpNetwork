using SciFiLarpNetwork.Backend.Domain.Skills;

namespace SciFiLarpNetwork.Backend.Tests.Domain.Skills;

public sealed class CatalogNamesTests
{
    [Theory]
    [InlineData("Hacking I")]
    [InlineData("  Medicina  ")]
    [InlineData("a")]
    public void ReasonableNamesAreValid(string name)
    {
        Assert.True(CatalogNames.IsValid(name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Tab\tinside")]
    public void BlankOrControlCharacterNamesAreInvalid(string? name)
    {
        Assert.False(CatalogNames.IsValid(name));
    }

    [Fact]
    public void NameLongerThanTheMaximumIsInvalid()
    {
        Assert.False(CatalogNames.IsValid(new string('a', CatalogNames.MaximumLength + 1)));
    }

    [Fact]
    public void NameAtTheMaximumLengthIsValid()
    {
        Assert.True(CatalogNames.IsValid(new string('a', CatalogNames.MaximumLength)));
    }

    [Fact]
    public void NormalizationIgnoresCaseSurroundingSpaceAndCompatibilityForms()
    {
        Assert.Equal(CatalogNames.Normalize("Ｈacking i"), CatalogNames.Normalize("  HACKING I "));
    }
}
