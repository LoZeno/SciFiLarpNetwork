using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace SciFiLarpNetwork.Management.Tests.Views;

/// <summary>Measures a ScrollViewer's own scroll bars against its content, in the coordinates of a common root.</summary>
internal static class ScrollBarLayout
{
    public static ScrollBar VerticalBar(ScrollViewer scroller) => Bar(scroller, "PART_VerticalScrollBar");

    public static ScrollBar HorizontalBar(ScrollViewer scroller) => Bar(scroller, "PART_HorizontalScrollBar");

    public static Rect BoundsIn(Visual root, Visual control) =>
        new(control.TranslatePoint(default, root)!.Value, control.Bounds.Size);

    /// <summary>The visible controls in the scrolled content whose bounds meet the bar's bounds.</summary>
    public static string[] ControlsUnder(Visual root, ScrollViewer scroller, ScrollBar bar)
    {
        var barBounds = BoundsIn(root, bar);
        return ((Visual)scroller.Content!).GetVisualDescendants().Prepend((Visual)scroller.Content!).OfType<Control>()
            .Where(control => control is TemplatedControl or TextBlock && control.IsEffectivelyVisible)
            .Where(control => BoundsIn(root, control).Intersects(barBounds))
            .Select(control => control.GetType().Name)
            .ToArray();
    }

    public static bool HasArrowButtons(ScrollBar bar) =>
        bar.GetVisualDescendants().OfType<RepeatButton>()
            .Any(button => button.Name is "PART_LineUpButton" or "PART_LineDownButton" && button.IsEffectivelyVisible);

    /// <summary>The bar's thickness across its direction: width for a vertical bar, height for a horizontal one.</summary>
    public static double Thickness(ScrollBar bar) =>
        bar.Orientation == Avalonia.Layout.Orientation.Vertical ? bar.Bounds.Width : bar.Bounds.Height;

    private static ScrollBar Bar(ScrollViewer scroller, string name) =>
        scroller.GetVisualDescendants().OfType<ScrollBar>()
            .Single(bar => bar.Name == name && ReferenceEquals(bar.TemplatedParent, scroller));
}
