using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace SciFiLarpNetwork.Management.Views.Layout;

/// <summary>
/// Content of a <see cref="ScreenScrollViewer"/> that takes exactly the scroll viewer's visible size, but never less than
/// its <see cref="Layoutable.MinWidth"/> and <see cref="Layoutable.MinHeight"/>. When the window is big enough nothing
/// scrolls and lists inside fill the space and scroll by themselves; when it is smaller the whole content scrolls.
/// </summary>
/// <remarks>
/// A plain scroll viewer gives its content unlimited room, so a list would grow to show every row and push the
/// buttons under it out of sight instead of scrolling. The visible size is the space the scroll viewer itself was
/// given (<see cref="ScreenScrollViewer.AvailableRoom"/>), which is known before its content is measured, so the
/// content is measured once, at the size it is shown at. Measuring it again during arrange leaves text drawn for the first size until something else repaints it.
/// Because Avalonia caps a child's desired size at the space offered, this control cannot learn how much its content
/// really needs: the minimum must be big enough for the content's fixed parts, and any part that can grow without
/// limit needs its own scroll viewer.
/// </remarks>
public sealed class FillViewport : Decorator
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var visible = VisibleSize();
        var size = new Size(
            Fill(availableSize.Width, visible?.Width, Margin.Left + Margin.Right, MinWidth),
            Fill(availableSize.Height, visible?.Height, Margin.Top + Margin.Bottom, MinHeight));
        Child?.Measure(size.Deflate(Padding));
        return MinimumSize(size);
    }

    /// <summary>
    /// Asks the scroll viewer only for the minimum, which is all that has to scroll; it stretches this control to the
    /// visible area anyway. Asking for the full room would push the window's layout out by a rounding pixel.
    /// </summary>
    private Size MinimumSize(Size room) => new(Math.Min(room.Width, MinWidth), Math.Min(room.Height, MinHeight));

    protected override Size ArrangeOverride(Size finalSize)
    {
        Child?.Arrange(new Rect(finalSize).Deflate(Padding));
        return finalSize;
    }

    private Size? VisibleSize() => this.FindAncestorOfType<ScreenScrollViewer>()?.AvailableRoom;

    private static double Fill(double available, double? visible, double margin, double minimum)
    {
        var offered = double.IsInfinity(available) ? (visible ?? 0) - margin : available;
        return Math.Max(double.IsInfinity(offered) ? 0 : offered, minimum);
    }
}
