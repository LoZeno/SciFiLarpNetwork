using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace SciFiLarpNetwork.Management.Views.Layout;

/// <summary>
/// A <see cref="ScrollViewer"/> that tells a <see cref="FillViewport"/> inside how much room it has before the content
/// is measured, so the content is laid out at the size it is shown at.
/// </summary>
/// <remarks>
/// The room is first taken from the space this scroll viewer is offered. On a scaled screen the size it is finally
/// given can be a pixel smaller after rounding; then the content is measured again in a normal layout pass, never
/// from inside arrange, where a new text layout would not be drawn until something else repaints.
/// </remarks>
public sealed class ScreenScrollViewer : ScrollViewer
{
    private Size _lastOffered;

    protected override Type StyleKeyOverride => typeof(ScrollViewer);

    /// <summary>The room the content has: the size this scroll viewer was given, or offered before it was first given one.</summary>
    public Size AvailableRoom { get; private set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_lastOffered != availableSize)
        {
            _lastOffered = availableSize;
            UseRoom(availableSize);
        }

        return base.MeasureOverride(availableSize);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        UseRoom(finalSize);
        return base.ArrangeOverride(finalSize);
    }

    private void UseRoom(Size room)
    {
        if (AvailableRoom == room)
        {
            return;
        }

        AvailableRoom = room;
        (Content as Layoutable)?.InvalidateMeasure();
    }
}
