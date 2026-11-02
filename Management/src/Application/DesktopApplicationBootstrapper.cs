using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using SciFiLarpNetwork.Management.Views;

namespace SciFiLarpNetwork.Management.Application;

public sealed class DesktopApplicationBootstrapper(Func<MainWindow> createMainWindow)
{
    public void Configure(IApplicationLifetime? lifetime)
    {
        if (lifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = createMainWindow();
        }
    }
}
