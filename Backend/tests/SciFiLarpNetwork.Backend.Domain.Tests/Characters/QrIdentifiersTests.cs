using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Tests.Domain.Characters;

public sealed class QrIdentifiersTests
{
    [Fact]
    public void CreatedIdentifiersAre32LowerCaseHexadecimalDigits()
    {
        var identifier = QrIdentifiers.Create();

        Assert.Matches("^[0-9a-f]{32}$", identifier);
    }

    [Theory]
    [InlineData("0B8F6C3E58D14A2F9C471E6D2A9B3F71")]
    [InlineData(" 0b8f6c3e-58d1-4a2f-9c47-1e6d2a9b3f71 ")]
    [InlineData("{0b8f6c3e-58d1-4a2f-9c47-1e6d2a9b3f71}")]
    public void StandardGuidFormsNormalizeToTheStoredForm(string input)
    {
        var parsed = QrIdentifiers.TryNormalize(input, out var identifier);

        Assert.Equal((true, "0b8f6c3e58d14a2f9c471e6d2a9b3f71"), (parsed, identifier));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-code")]
    public void OtherTextIsNotAnIdentifier(string? input)
    {
        Assert.False(QrIdentifiers.TryNormalize(input, out _));
    }
}
