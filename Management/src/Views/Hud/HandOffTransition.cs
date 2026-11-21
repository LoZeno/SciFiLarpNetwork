using Avalonia.Animation;
using Avalonia.Animation.Easings;

namespace SciFiLarpNetwork.Management.Views.Hud;

/// <summary>
/// Animates a 0–1 reveal progress so that two reveals sharing one slot hand over cleanly: a closing reveal runs
/// in the first half of the duration and an opening one waits for it and runs in the second half.
/// </summary>
public sealed class HandOffTransition : InterpolatingTransitionBase<double>
{
    private static readonly CubicEaseInOut Curve = new();

    /// <summary>False makes the progress run over the whole duration in both directions, with no waiting.</summary>
    public bool WaitsForPartner { get; set; } = true;

    protected override double Interpolate(double progress, double from, double to)
    {
        var phase = !WaitsForPartner ? progress
            : to > from ? Math.Clamp(progress * 2 - 1, 0, 1)
            : Math.Clamp(progress * 2, 0, 1);
        return from + (to - from) * Curve.Ease(phase);
    }
}
