using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace SciFiLarpNetwork.Management.Views.Converters;

/// <summary>
/// Turns encoded image bytes (PNG, JPEG, or WebP) into a bitmap. Anything that fails to decode shows no image,
/// because the server checks only the image header and a damaged file must never crash the dialog.
/// </summary>
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
#pragma warning disable CA1031 // Any decode failure from the native image codec means "no preview"; nothing here can recover more specifically.
        catch (Exception)
#pragma warning restore CA1031
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
