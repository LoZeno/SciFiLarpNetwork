using SciFiLarpNetwork.Backend.Domain.Players;

namespace SciFiLarpNetwork.Backend.Tests.Domain.Players;

public sealed class PlayerContactsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    [InlineData("ann@example.org")]
    [InlineData(" ann.b+larp@example.co.uk ")]
    public void EmptyOrWellFormedEmailIsValid(string? email)
    {
        Assert.True(PlayerContacts.IsValidEmail(email));
    }

    [Theory]
    [InlineData("ann")]
    [InlineData("ann@")]
    [InlineData("ann example@example.org")]
    [InlineData("Ann <ann@example.org>")]
    public void MalformedEmailIsInvalid(string email)
    {
        Assert.False(PlayerContacts.IsValidEmail(email));
    }

    [Fact]
    public void EmailLongerThan254CharactersIsInvalid()
    {
        Assert.False(PlayerContacts.IsValidEmail($"{new string('a', 64)}@{new string('b', 186)}.org"));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("+39 333 000 0000", true)]
    [InlineData("12345678901234567890123456789012", true)]
    [InlineData("123456789012345678901234567890123", false)]
    [InlineData("555\n0100", false)]
    public void PhoneIsUpTo32CharactersWithoutControlCharacters(string? phone, bool expected)
    {
        Assert.Equal(expected, PlayerContacts.IsValidPhone(phone));
    }

    [Fact]
    public void NotesMayHoldLineBreaksUpTo2000Characters()
    {
        Assert.Equal(
            new { Fits = true, TooLong = false },
            new
            {
                Fits = PlayerContacts.IsValidNotes(new string('n', 1999) + "\n"),
                TooLong = PlayerContacts.IsValidNotes(new string('n', 2001))
            });
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    [InlineData(" Ann ", "Ann")]
    public void NormalizeTrimsAndTreatsBlankAsNotSet(string? value, string? expected)
    {
        Assert.Equal(expected, PlayerContacts.Normalize(value));
    }
}
