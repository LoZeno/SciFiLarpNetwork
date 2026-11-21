using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace SciFiLarpNetwork.Management.Views.Hud;

/// <summary>
/// A border whose chosen corners are cut at 45 degrees, with an optional soft glow around the edge and a bright
/// accent bar along the start of the top edge.
/// </summary>
public sealed class ChamferBorder : Decorator
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<ChamferBorder, IBrush?>(nameof(Background));

    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.Register<ChamferBorder, IBrush?>(nameof(BorderBrush));

    public static readonly StyledProperty<double> BorderThicknessProperty =
        AvaloniaProperty.Register<ChamferBorder, double>(nameof(BorderThickness), 1);

    public static readonly StyledProperty<double> ChamferProperty =
        AvaloniaProperty.Register<ChamferBorder, double>(nameof(Chamfer), 12);

    public static readonly StyledProperty<HudCorners> CornersProperty =
        AvaloniaProperty.Register<ChamferBorder, HudCorners>(nameof(Corners), HudCorners.TopLeft | HudCorners.BottomRight);

    public static readonly StyledProperty<double> GlowProperty =
        AvaloniaProperty.Register<ChamferBorder, double>(nameof(Glow));

    public static readonly StyledProperty<double> AccentLengthProperty =
        AvaloniaProperty.Register<ChamferBorder, double>(nameof(AccentLength));

    static ChamferBorder() =>
        AffectsRender<ChamferBorder>(
            BackgroundProperty, BorderBrushProperty, BorderThicknessProperty, ChamferProperty, CornersProperty, GlowProperty,
            AccentLengthProperty);

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public double BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    /// <summary>How far each cut corner reaches along its two edges, in pixels.</summary>
    public double Chamfer
    {
        get => GetValue(ChamferProperty);
        set => SetValue(ChamferProperty, value);
    }

    public HudCorners Corners
    {
        get => GetValue(CornersProperty);
        set => SetValue(CornersProperty, value);
    }

    /// <summary>The width of the soft halo drawn around the edge; 0 draws none.</summary>
    public double Glow
    {
        get => GetValue(GlowProperty);
        set => SetValue(GlowProperty, value);
    }

    /// <summary>The length of the thick bar drawn along the start of the top edge; 0 draws none.</summary>
    public double AccentLength
    {
        get => GetValue(AccentLengthProperty);
        set => SetValue(AccentLengthProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var inset = BorderThickness / 2;
        var outline = new Rect(Bounds.Size).Deflate(inset);
        if (outline.Width <= 0 || outline.Height <= 0)
        {
            return;
        }

        var geometry = Outline(outline);
        DrawGlow(context, geometry);
        context.DrawGeometry(Background, BorderBrush is null ? null : new Pen(BorderBrush, BorderThickness), geometry);
        DrawAccent(context, outline);
    }

    private void DrawGlow(DrawingContext context, Geometry geometry)
    {
        if (Glow <= 0 || BorderBrush is null)
        {
            return;
        }

        using (context.PushOpacity(0.12))
        {
            context.DrawGeometry(null, new Pen(BorderBrush, BorderThickness + Glow * 2), geometry);
        }

        using (context.PushOpacity(0.25))
        {
            context.DrawGeometry(null, new Pen(BorderBrush, BorderThickness + Glow), geometry);
        }
    }

    private void DrawAccent(DrawingContext context, Rect outline)
    {
        if (AccentLength <= 0 || BorderBrush is null)
        {
            return;
        }

        var start = outline.Left + Cut(HudCorners.TopLeft, outline);
        var end = Math.Min(start + AccentLength, outline.Right - Cut(HudCorners.TopRight, outline));
        context.DrawLine(new Pen(BorderBrush, BorderThickness + 2), new Point(start, outline.Top), new Point(end, outline.Top));
    }

    private StreamGeometry Outline(Rect area)
    {
        var geometry = new StreamGeometry();
        using var figure = geometry.Open();
        figure.BeginFigure(new Point(area.Left + Cut(HudCorners.TopLeft, area), area.Top), true);
        figure.LineTo(new Point(area.Right - Cut(HudCorners.TopRight, area), area.Top));
        figure.LineTo(new Point(area.Right, area.Top + Cut(HudCorners.TopRight, area)));
        figure.LineTo(new Point(area.Right, area.Bottom - Cut(HudCorners.BottomRight, area)));
        figure.LineTo(new Point(area.Right - Cut(HudCorners.BottomRight, area), area.Bottom));
        figure.LineTo(new Point(area.Left + Cut(HudCorners.BottomLeft, area), area.Bottom));
        figure.LineTo(new Point(area.Left, area.Bottom - Cut(HudCorners.BottomLeft, area)));
        figure.LineTo(new Point(area.Left, area.Top + Cut(HudCorners.TopLeft, area)));
        figure.EndFigure(true);
        return geometry;
    }

    private double Cut(HudCorners corner, Rect area) =>
        Corners.HasFlag(corner) ? Math.Min(Chamfer, Math.Min(area.Width, area.Height) / 2) : 0;
}
