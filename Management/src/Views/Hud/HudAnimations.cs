using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace SciFiLarpNetwork.Management.Views.Hud;

public static class HudAnimations
{
    private static readonly TimeSpan EntryStagger = TimeSpan.FromMilliseconds(40);

    /// <summary>Drops a list entry into place from just above it, later for each <paramref name="index"/>.</summary>
    public static Task DropInAsync(Control entry, int index) =>
        Move(TimeSpan.FromMilliseconds(220), EntryStagger * index, TranslateTransform.YProperty, -10, fromOpacity: 0)
            .RunAsync(entry);

    /// <summary>Slides a panel in from beyond its left edge.</summary>
    public static Task SlideInFromLeftAsync(Control panel) =>
        Move(TimeSpan.FromMilliseconds(320), TimeSpan.Zero, TranslateTransform.XProperty, -Math.Max(panel.Bounds.Width, 1), 0.2)
            .RunAsync(panel);

    private static Animation Move(
        TimeSpan duration, TimeSpan delay, AvaloniaProperty axis, double fromOffset, double fromOpacity) =>
        new()
        {
            Duration = duration,
            Delay = delay,
            Easing = new CubicEaseOut(),
            FillMode = FillMode.Backward,
            Children =
            {
                Frame(0, axis, fromOffset, fromOpacity),
                Frame(1, axis, 0, 1)
            }
        };

    private static KeyFrame Frame(double cue, AvaloniaProperty axis, double offset, double opacity) =>
        new()
        {
            Cue = new Cue(cue),
            Setters =
            {
                new Setter(axis, offset),
                new Setter(Visual.OpacityProperty, opacity)
            }
        };
}
