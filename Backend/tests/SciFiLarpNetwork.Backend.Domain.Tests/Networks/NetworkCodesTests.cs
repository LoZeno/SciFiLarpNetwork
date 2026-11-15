using SciFiLarpNetwork.Backend.Domain.Networks;

namespace SciFiLarpNetwork.Backend.Tests.Domain.Networks;

public sealed class NetworkCodesTests
{
    [Theory]
    [InlineData("rebels")]
    [InlineData("outer-rim-2")]
    [InlineData("  Core  ")]
    [InlineData("a")]
    public void WellFormedCodesAreValid(string code)
    {
        Assert.True(NetworkCodes.IsValid(code));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-rebels")]
    [InlineData("rebels-")]
    [InlineData("outer rim")]
    [InlineData("outer_rim")]
    [InlineData("rete-è")]
    public void MalformedCodesAreInvalid(string? code)
    {
        Assert.False(NetworkCodes.IsValid(code));
    }

    [Fact]
    public void CodeLongerThanTheMaximumIsInvalid()
    {
        Assert.False(NetworkCodes.IsValid(new string('a', NetworkCodes.MaximumLength + 1)));
    }

    [Fact]
    public void CodeAtTheMaximumLengthIsValid()
    {
        Assert.True(NetworkCodes.IsValid(new string('a', NetworkCodes.MaximumLength)));
    }

    [Fact]
    public void NormalizationTrimsAndLowerCases()
    {
        Assert.Equal("outer-rim", NetworkCodes.Normalize("  Outer-RIM "));
    }
}
