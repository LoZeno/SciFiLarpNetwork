using System.Globalization;
using Avalonia.Data.Converters;

namespace SciFiLarpNetwork.Management.Views.Converters;

/// <summary>Turns a heading level into a font size; other text keeps the normal size.</summary>
public sealed class HeadingSizeConverter : IValueConverter
{
    private const double NormalSize = 14;

    public static HeadingSizeConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int level and >= 1 and <= 6 ? NormalSize + ((7 - level) * 2) : NormalSize;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
