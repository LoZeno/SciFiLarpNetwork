using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace SciFiLarpNetwork.Management.Tests.Application;

/// <remarks>
/// Uses the Skia renderer rather than the headless fake drawing backend, so bitmaps are really decoded and tests
/// can tell a valid image from a broken one (Avalonia docs, "Headless Testing Platform", Enabling the Skia renderer).
/// </remarks>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
