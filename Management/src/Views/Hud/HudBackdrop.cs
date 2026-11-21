using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace SciFiLarpNetwork.Management.Views.Hud;

/// <summary>A faint repeating texture drawn behind HUD content: a dot grid or diagonal stripes.</summary>
public sealed class HudBackdrop : Control
{
    public static readonly StyledProperty<HudPattern> PatternProperty =
        AvaloniaProperty.Register<HudBackdrop, HudPattern>(nameof(Pattern));

    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<HudBackdrop, double>(nameof(Spacing), 18);

    public static readonly StyledProperty<IBrush?> InkProperty =
        AvaloniaProperty.Register<HudBackdrop, IBrush?>(nameof(Ink));

    static HudBackdrop()
    {
        AffectsRender<HudBackdrop>(PatternProperty, SpacingProperty, InkProperty);
        IsHitTestVisibleProperty.OverrideDefaultValue<HudBackdrop>(false);
    }

    public HudPattern Pattern
    {
        get => GetValue(PatternProperty);
        set => SetValue(PatternProperty, value);
    }

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public IBrush? Ink
    {
        get => GetValue(InkProperty);
        set => SetValue(InkProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        if (Ink is null || Spacing < 2)
        {
            return;
        }

        using (context.PushClip(new Rect(Bounds.Size)))
        {
            if (Pattern == HudPattern.Dots)
            {
                DrawDots(context, Ink);
            }
            else
            {
                DrawStripes(context, Ink);
            }
        }
    }

    private void DrawDots(DrawingContext context, IBrush ink)
    {
        for (var y = Spacing / 2; y < Bounds.Height; y += Spacing)
        {
            for (var x = Spacing / 2; x < Bounds.Width; x += Spacing)
            {
                context.FillRectangle(ink, new Rect(x, y, 1.5, 1.5));
            }
        }
    }

    private void DrawStripes(DrawingContext context, IBrush ink)
    {
        var pen = new Pen(ink, 2);
        for (var x = 0d; x < Bounds.Width + Bounds.Height; x += Spacing)
        {
            context.DrawLine(pen, new Point(x, 0), new Point(x - Bounds.Height, Bounds.Height));
        }
    }
}
