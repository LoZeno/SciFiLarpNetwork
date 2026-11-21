using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace SciFiLarpNetwork.Management.Views.Hud;

public static class HudAnimations
{
    /// <summary>The time a panel takes to slide out beyond its left edge.</summary>
    public static readonly TimeSpan SlideOutDuration = TimeSpan.FromMilliseconds(200);

    private static readonly TimeSpan SlideInDuration = TimeSpan.FromMilliseconds(320);
    private static readonly TimeSpan EntryStagger = TimeSpan.FromMilliseconds(40);

    /// <summary>Drops a list entry into place from just above it, later for each <paramref name="index"/>.</summary>
    public static Task DropInAsync(Control entry, int index) =>
        Move(TimeSpan.FromMilliseconds(220), EntryStagger * index, TranslateTransform.YProperty, (-10, 0), (0, 1), new CubicEaseOut())
            .RunAsync(entry);

    /// <summary>
    /// Slides a panel in from beyond its left edge once <paramref name="delay"/> has passed; until then it waits out of
    /// sight beyond that edge.
    /// </summary>
    public static Task SlideInFromLeftAsync(Control panel, TimeSpan delay, CancellationToken cancellationToken) =>
        Move(SlideInDuration, delay, TranslateTransform.XProperty, (-OffPanel(panel), 0), (0.2, 1), new CubicEaseOut())
            .RunAsync(panel, cancellationToken);

    /// <summary>
    /// Slides a panel left by <paramref name="distance"/>, out of sight; it stays there until the caller hides it.
    /// </summary>
    public static Task SlideOutToLeftAsync(Control panel, double distance, CancellationToken cancellationToken)
    {
        var slide = Move(SlideOutDuration, TimeSpan.Zero, TranslateTransform.XProperty, (0, -distance), (1, 0.2), new CubicEaseOut());
        slide.FillMode = FillMode.Both;
        return slide.RunAsync(panel, cancellationToken);
    }

    private static double OffPanel(Control panel) => Math.Max(panel.Bounds.Width, 1);

    private static Animation Move(
        TimeSpan duration,
        TimeSpan delay,
        AvaloniaProperty axis,
        (double From, double To) offset,
        (double From, double To) opacity,
        Easing easing) =>
        new()
        {
            Duration = duration,
            Delay = delay,
            Easing = easing,
            FillMode = FillMode.Backward,
            Children =
            {
                Frame(0, axis, offset.From, opacity.From),
                Frame(1, axis, offset.To, opacity.To)
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
