namespace SciFiLarpNetwork.Management.Tests.Application;

public sealed class AppBuilderConfigurationTests
{
    [Fact]
    public void BuildReturnsAvaloniaApplicationBuilder()
    {
        Assert.NotNull(AppBuilderConfiguration.Build());
    }
}
