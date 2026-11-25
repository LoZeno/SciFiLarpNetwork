using System.Globalization;

namespace SciFiLarpNetwork.Management.ViewModels.News;

public static class NewsSizes
{
    private const double Kilobyte = 1024;
    private const double Megabyte = 1024 * 1024;

    /// <summary>A file size in bytes, KB, or MB, as people read it.</summary>
    public static string Format(long byteCount) =>
        byteCount >= Megabyte ? string.Format(CultureInfo.CurrentCulture, "{0:0.#} MB", byteCount / Megabyte)
        : byteCount >= Kilobyte ? string.Format(CultureInfo.CurrentCulture, "{0:0.#} KB", byteCount / Kilobyte)
        : string.Format(CultureInfo.CurrentCulture, "{0} B", byteCount);
}
