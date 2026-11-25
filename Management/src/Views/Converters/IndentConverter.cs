using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace SciFiLarpNetwork.Management.Views.Converters;

/// <summary>Turns a list or quote depth into a left margin.</summary>
public sealed class IndentConverter : IValueConverter
{
    private const double Step = 20;

    public static IndentConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int indent ? new Thickness(indent * Step, 0, 0, 0) : new Thickness(0);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
