using SciFiLarpNetwork.Backend.Domain.Items;

namespace SciFiLarpNetwork.Backend.Tests.Domain.Items;

public sealed class ItemTextsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Heals two wounds.\nOne use.")]
    public void BlankOrOrdinaryTextIsValid(string? text)
    {
        Assert.True(ItemTexts.IsValidText(text));
    }

    [Fact]
    public void TextAtTheMaximumLengthIsValid()
    {
        Assert.True(ItemTexts.IsValidText(new string('a', ItemTexts.MaximumTextLength)));
    }

    [Fact]
    public void TextLongerThanTheMaximumIsInvalid()
    {
        Assert.False(ItemTexts.IsValidText(new string('a', ItemTexts.MaximumTextLength + 1)));
    }

    [Fact]
    public void TextWithANullCharacterIsInvalid()
    {
        Assert.False(ItemTexts.IsValidText("bad\0text"));
    }

    [Fact]
    public void BlankTextNormalizesToNotSet()
    {
        Assert.Null(ItemTexts.Normalize("   "));
    }

    [Fact]
    public void TextIsTrimmed()
    {
        Assert.Equal("Heals", ItemTexts.Normalize("  Heals  "));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(ItemTexts.MaximumCost, true)]
    [InlineData(-1, false)]
    [InlineData(ItemTexts.MaximumCost + 1, false)]
    public void CostMustBeInRange(int cost, bool valid)
    {
        Assert.Equal(valid, ItemTexts.IsValidCost(cost));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(1, true)]
    [InlineData(ItemTexts.MaximumCharges, true)]
    [InlineData(0, false)]
    [InlineData(ItemTexts.MaximumCharges + 1, false)]
    public void ChargesMustBeUnsetOrInRange(int? charges, bool valid)
    {
        Assert.Equal(valid, ItemTexts.IsValidCharges(charges));
    }
}
