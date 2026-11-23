using SciFiLarpNetwork.Backend.Domain.Messages;

namespace SciFiLarpNetwork.Backend.Tests.Domain.Messages;

public sealed class MessageTextsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Supply run\u0007")]
    public void MissingOrControlTitleIsInvalid(string? title) =>
        Assert.False(MessageTexts.IsValidTitle(title));

    [Fact]
    public void TitleOf128CharactersAfterTrimmingIsValid() =>
        Assert.True(MessageTexts.IsValidTitle($"  {new string('t', 128)}  "));

    [Fact]
    public void TitleLongerThan128CharactersIsInvalid() =>
        Assert.False(MessageTexts.IsValidTitle(new string('t', 129)));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \r\n\t ")]
    public void MissingOrBlankBodyIsInvalid(string? body) =>
        Assert.False(MessageTexts.IsValidBody(body));

    [Fact]
    public void BodyWithLineBreaksIsValid() =>
        Assert.True(MessageTexts.IsValidBody("Meet at the dock.\r\n\r\n  — K."));

    [Fact]
    public void BodyOf10000CharactersIsValid() =>
        Assert.True(MessageTexts.IsValidBody(new string('b', 10000)));

    [Fact]
    public void BodyLongerThan10000CharactersIsInvalid() =>
        Assert.False(MessageTexts.IsValidBody(new string('b', 10001)));
}
