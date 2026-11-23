using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using SciFiLarpNetwork.Management.Views.Layout;

namespace SciFiLarpNetwork.Management.Tests.Views;

public sealed class FillViewportTests
{
    [Fact]
    public Task PaddingInsetsTheContent() => AvaloniaUi.Run(() =>
    {
        var content = new Border();
        var window = new Window
        {
            Width = 400,
            Height = 300,
            Content = new ScreenScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, Content = new FillViewport { Padding = new Thickness(10), Child = content } }
        };
        window.Show();
        window.UpdateLayout();

        window.Close();
        Assert.Equal(new Rect(10, 10, 380, 280), content.Bounds);
    });

    /// <summary>
    /// Laying the content out again at another size after the window decided what to draw leaves text drawn for the
    /// old size, so the content is only ever measured at the size it is shown at.
    /// </summary>
    [Fact]
    public Task ContentIsOnlyMeasuredAtTheSizeItIsShownAt() => AvaloniaUi.Run(() =>
    {
        var content = new MeasureRecorder();
        var window = new Window
        {
            Width = 400,
            Height = 300,
            Content = new ScreenScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, Content = new FillViewport { MinWidth = 100, MinHeight = 100, Child = content } }
        };
        window.Show();
        window.UpdateLayout();

        window.Close();
        Assert.Equal([new Size(400, 300)], content.Constraints.Distinct());
    });

    [Fact]
    public Task ResizedWindowMeasuresTheContentOnlyAtTheNewSize() => AvaloniaUi.Run(() =>
    {
        var content = new MeasureRecorder();
        var window = new Window
        {
            Width = 400,
            Height = 300,
            Content = new ScreenScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, Content = new FillViewport { MinWidth = 100, MinHeight = 100, Child = content } }
        };
        window.Show();
        window.UpdateLayout();
        content.Constraints.Clear();

        window.Width = 600;
        window.Height = 500;
        window.UpdateLayout();

        window.Close();
        Assert.Equal([new Size(600, 500)], content.Constraints.Distinct());
    });

    private sealed class MeasureRecorder : Control
    {
        public List<Size> Constraints { get; } = [];

        protected override Size MeasureOverride(Size availableSize)
        {
            Constraints.Add(availableSize);
            return default;
        }
    }
}
