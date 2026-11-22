using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using SciFiLarpNetwork.Management.Views.Layout;

namespace SciFiLarpNetwork.Management.Tests.Views;

public sealed class FillViewportTests
{
    [AvaloniaFact]
    public void PaddingInsetsTheContent()
    {
        var content = new Border();
        var window = new Window
        {
            Width = 400,
            Height = 300,
            Content = new ScrollViewer { Content = new FillViewport { Padding = new Thickness(10), Child = content } }
        };
        window.Show();
        window.UpdateLayout();

        window.Close();
        Assert.Equal(new Rect(10, 10, 380, 280), content.Bounds);
    }
}
