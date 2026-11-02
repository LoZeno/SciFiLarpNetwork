using Avalonia;

namespace SciFiLarpNetwork.Management;

public static class AppBuilderConfiguration
{
    public static AppBuilder Build() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
