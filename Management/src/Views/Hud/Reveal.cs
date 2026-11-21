using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;

namespace SciFiLarpNetwork.Management.Views.Hud;

/// <summary>
/// Shows or hides its content with an animated fold or squeeze. A hidden reveal takes no input, and its content
/// becomes invisible (out of the tab order) once fully closed. Changes made before the reveal is on screen apply
/// at once, so a window opens already in its final state.
/// </summary>
public sealed class Reveal : Decorator
{
    /// <summary>The time one fold or squeeze takes; a reveal waiting for its partner takes twice as long in total.</summary>
    public static readonly TimeSpan StepDuration = TimeSpan.FromMilliseconds(250);

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<Reveal, bool>(nameof(IsExpanded), true);

    public static readonly StyledProperty<RevealMode> ModeProperty =
        AvaloniaProperty.Register<Reveal, RevealMode>(nameof(Mode));

    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<Reveal, double>(nameof(Progress), 1);

    private readonly HandOffTransition _transition = new() { Property = ProgressProperty, Duration = StepDuration * 2 };

    static Reveal()
    {
        AffectsMeasure<Reveal>(ProgressProperty, ModeProperty);
        ClipToBoundsProperty.OverrideDefaultValue<Reveal>(true);
    }

    public Reveal() => Transitions = [_transition];

    /// <summary>Raised when a closing reveal has fully folded or squeezed away.</summary>
    public event EventHandler? Closed;

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public RevealMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    /// <summary>How much of the content shows, from 0 (hidden) to 1 (fully shown); animated after each change of
    /// <see cref="IsExpanded"/>.</summary>
    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <summary>
    /// True (the default) makes an opening reveal wait for a closing partner in the same slot before it opens;
    /// false makes it open at once over the full duration.
    /// </summary>
    public bool WaitsForPartner
    {
        get => _transition.WaitsForPartner;
        set => _transition.WaitsForPartner = value;
    }

    /// <summary>True while the reveal is animating towards fully shown.</summary>
    public bool IsOpening => IsExpanded && Progress < 1;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsExpandedProperty)
        {
            IsHitTestVisible = IsExpanded;
            Progress = IsExpanded ? 1 : 0;
            ShowContentWhileVisible();
        }
        else if (change.Property == ProgressProperty)
        {
            ApplyProgress();
            ShowContentWhileVisible();
            RaiseClosedWhenFolded();
        }
        else if (change.Property == ChildProperty || change.Property == ModeProperty)
        {
            ApplyProgress();
            ShowContentWhileVisible();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        return Mode == RevealMode.Fold ? size.WithHeight(size.Height * Progress) : size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Mode == RevealMode.Squeeze || Child is null)
        {
            return base.ArrangeOverride(finalSize);
        }

        var width = Math.Max(0, finalSize.Width - Padding.Left - Padding.Right);
        Child.Arrange(new Rect(Padding.Left, Padding.Top, width, Child.DesiredSize.Height));
        return finalSize;
    }

    private void ApplyProgress()
    {
        Opacity = Progress;
        RenderTransform = Mode == RevealMode.Squeeze ? new ScaleTransform(Progress, 1) : null;
    }

    private void ShowContentWhileVisible()
    {
        if (Child is not null)
        {
            Child.IsVisible = IsExpanded || Progress > 0;
        }
    }

    private void RaiseClosedWhenFolded()
    {
        if (!IsExpanded && Progress <= 0)
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }
    }
}
