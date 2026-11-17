using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Tests.Domain.Characters;

public sealed class CharacterTextsTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("  ", true)]
    [InlineData("Human", true)]
    [InlineData("tab\there", false)]
    public void OptionalTextIsBlankOrAValidName(string? text, bool expected)
    {
        Assert.Equal(expected, CharacterTexts.IsValidOptional(text));
    }

    [Fact]
    public void TextLongerThan64CharactersIsInvalid()
    {
        Assert.False(CharacterTexts.IsValidOptional(new string('x', 65)));
    }
}
