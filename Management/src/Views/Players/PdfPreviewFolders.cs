namespace SciFiLarpNetwork.Management.Views.Players;

/// <summary>
/// Creates one folder per PDF preview under <paramref name="root"/>. A preview folder cannot be deleted when the
/// viewer opens it, because the viewer may still read the file; instead each new preview deletes the preview folders
/// last written more than a day ago.
/// </summary>
public sealed class PdfPreviewFolders(string root)
{
    private const string Prefix = "scifilarp-card-preview-";
    private static readonly TimeSpan MaxAge = TimeSpan.FromDays(1);

    public DirectoryInfo Create()
    {
        DeleteOldFolders();
        return Directory.CreateDirectory(Path.Combine(root, Prefix + Guid.NewGuid().ToString("N")));
    }

    private void DeleteOldFolders()
    {
        var cutoff = DateTime.UtcNow - MaxAge;
        foreach (var folder in new DirectoryInfo(root).EnumerateDirectories(Prefix + "*"))
        {
            if (folder.LastWriteTimeUtc < cutoff)
            {
                TryDelete(folder);
            }
        }
    }

    // A folder still open in a viewer is left for a later preview.
    private static void TryDelete(DirectoryInfo folder)
    {
        try
        {
            folder.Delete(recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
