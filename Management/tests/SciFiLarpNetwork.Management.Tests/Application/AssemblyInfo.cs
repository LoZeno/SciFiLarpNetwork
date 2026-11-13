using Avalonia;
using Avalonia.Headless;
using SciFiLarpNetwork.Management.Tests.Application;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace SciFiLarpNetwork.Management.Tests.Application;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
