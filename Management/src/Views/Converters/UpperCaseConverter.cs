using System.Globalization;
using Avalonia.Data.Converters;

namespace SciFiLarpNetwork.Management.Views.Converters;

/// <summary>Shows text in capitals for HUD headings and buttons; anything that is not text passes through.</summary>
public sealed class UpperCaseConverter : IValueConverter
{
    public static UpperCaseConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string text ? text.ToUpper(culture) : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
