using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Tests.Domain.Characters;

public sealed class CharacterStatValuesTests
{
    [Theory]
    [InlineData(CharacterStatType.Number, 0, null, true)]
    [InlineData(CharacterStatType.Number, int.MaxValue, null, true)]
    [InlineData(CharacterStatType.Number, -1, null, false)]
    [InlineData(CharacterStatType.Number, null, null, false)]
    [InlineData(CharacterStatType.Number, 1, "one", false)]
    [InlineData(CharacterStatType.Text, null, null, true)]
    [InlineData(CharacterStatType.Text, null, "  ", true)]
    [InlineData(CharacterStatType.Text, null, "adept", true)]
    [InlineData(CharacterStatType.Text, 1, "adept", false)]
    [InlineData((CharacterStatType)9, 1, null, false)]
    public void ValueMustMatchTheStatType(CharacterStatType type, int? number, string? text, bool expected)
    {
        Assert.Equal(expected, CharacterStatValues.IsValid(type, number, text));
    }

    [Fact]
    public void TextLongerThan64CharactersIsInvalid()
    {
        Assert.False(CharacterStatValues.IsValid(CharacterStatType.Text, null, new string('x', 65)));
    }

    [Fact]
    public void NormalizeTrimsTextAndTreatsBlankAsNotSet()
    {
        var statId = Guid.NewGuid();

        Assert.Equal(
            new { Trimmed = new CharacterStatValue(statId, null, "adept"), Blank = new CharacterStatValue(statId, null, null) },
            new
            {
                Trimmed = CharacterStatValues.Normalize(statId, null, " adept "),
                Blank = CharacterStatValues.Normalize(statId, null, "  ")
            });
    }
}
