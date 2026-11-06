using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;

[assembly: AvaloniaTestApplication(typeof(SciFiLarpNetwork.Management.Tests.Application.TestAppBuilder))]

namespace SciFiLarpNetwork.Management.Tests.Application;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<SciFiLarpNetwork.Management.App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
