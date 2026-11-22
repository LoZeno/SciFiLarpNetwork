using Avalonia;
using Avalonia.Controls;

namespace SciFiLarpNetwork.Management.Views.Layout;

/// <summary>
/// Content of a <see cref="ScrollViewer"/> that takes exactly the scroll viewer's visible size, but never less than
/// its <see cref="Layoutable.MinWidth"/> and <see cref="Layoutable.MinHeight"/>. When the window is big enough nothing
/// scrolls and lists inside fill the space and scroll by themselves; when it is smaller the whole content scrolls.
/// </summary>
/// <remarks>
/// A plain scroll viewer gives its content unlimited room, so a list would grow to show every row and push the
/// buttons under it out of sight instead of scrolling. This control asks only for its minimum size; the scroll
/// viewer then arranges it at the larger of that and the visible size, and the content is measured again for that
/// size before it is arranged. Because Avalonia caps a child's desired size at the space offered, this control cannot
/// learn how much its content really needs: the minimum must be big enough for the content's fixed parts, and any
/// part that can grow without limit needs its own scroll viewer.
/// </remarks>
public sealed class FillViewport : Decorator
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var minimum = new Size(Finite(availableSize.Width, MinWidth), Finite(availableSize.Height, MinHeight));
        Child?.Measure(minimum.Deflate(Padding));
        return minimum;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var content = new Rect(finalSize).Deflate(Padding);
        Child?.Measure(content.Size);
        Child?.Arrange(content);
        return finalSize;
    }

    private static double Finite(double available, double minimum) => double.IsInfinity(available) ? minimum : available;
}
