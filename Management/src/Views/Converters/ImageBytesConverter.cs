using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace SciFiLarpNetwork.Management.Views.Converters;

/// <summary>Turns encoded image bytes (PNG, JPEG, or WebP) into a bitmap; anything unreadable shows no image.</summary>
public sealed class ImageBytesConverter : IValueConverter
{
    public static ImageBytesConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] { Length: > 0 } content)
        {
            return null;
        }

        try
        {
            using var stream = new MemoryStream(content, writable: false);
            return new Bitmap(stream);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NotSupportedException)
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
