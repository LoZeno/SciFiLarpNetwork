namespace SciFiLarpNetwork.Backend.Infrastructure.Media;

/// <summary>
/// The media folder (<c>Media:RootPath</c>), with one subfolder per feature. Each file is named by its identifier only,
/// so no uploaded name ever reaches a path, and subfolder names are fixed here, never taken from a request. An upload is
/// written under a temporary name and given its final name only once it is checked.
/// </summary>
internal sealed class MediaFileStore(string rootPath)
{
    private const int CopyBufferSize = 81920;

    /// <summary>
    /// Copies an upload to its temporary file, stopping once it exceeds <paramref name="maximumByteCount"/>.
    /// </summary>
    /// <returns>The byte count, or <see langword="null"/> when the upload is larger than allowed.</returns>
    public async Task<long?> WriteTemporaryAsync(MediaFolder folder, Guid fileId, Stream content, long maximumByteCount,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(FolderPath(folder));
        var buffer = new byte[CopyBufferSize];
        long total = 0;
        await using var file = new FileStream(TemporaryPath(folder, fileId), FileMode.CreateNew, FileAccess.Write,
            FileShare.None, CopyBufferSize, useAsync: true);
        int read;
        while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total += read;
            if (total > maximumByteCount)
            {
                return null;
            }

            await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return total;
    }

    /// <summary>Reads up to <paramref name="count"/> bytes from the start of the temporary file.</summary>
    public async Task<byte[]> ReadTemporaryStartAsync(MediaFolder folder, Guid fileId, long count,
        CancellationToken cancellationToken)
    {
        await using var file = File.OpenRead(TemporaryPath(folder, fileId));
        var buffer = new byte[(int)Math.Min(count, file.Length)];
        await file.ReadExactlyAsync(buffer, cancellationToken);
        return buffer;
    }

    public void CommitTemporary(MediaFolder folder, Guid fileId) =>
        File.Move(TemporaryPath(folder, fileId), FinalPath(folder, fileId));

    public void DeleteTemporary(MediaFolder folder, Guid fileId) => DeleteIfPresent(TemporaryPath(folder, fileId));

    public void Delete(MediaFolder folder, Guid fileId) => DeleteIfPresent(FinalPath(folder, fileId));

    /// <summary>
    /// A missing file is already deleted. On Windows a missing folder throws, which during clean-up after a failed write
    /// would hide the error that made the write fail.
    /// </summary>
    private static void DeleteIfPresent(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (DirectoryNotFoundException)
        {
        }
    }

    /// <summary>Opens a stored file for reading, or returns <see langword="null"/> when it is missing.</summary>
    public Stream? Open(MediaFolder folder, Guid fileId)
    {
        try
        {
            return new FileStream(FinalPath(folder, fileId), FileMode.Open, FileAccess.Read,
                FileShare.Read | FileShare.Delete, CopyBufferSize, useAsync: true);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }

    private string FolderPath(MediaFolder folder) => Path.Combine(rootPath, FolderName(folder));

    private string FinalPath(MediaFolder folder, Guid fileId) => Path.Combine(FolderPath(folder), fileId.ToString("D"));

    private string TemporaryPath(MediaFolder folder, Guid fileId) => Path.Combine(FolderPath(folder), $"{fileId:D}.upload");

    private static string FolderName(MediaFolder folder) => folder switch
    {
        MediaFolder.News => "News",
        MediaFolder.NetworkImages => "NetworkImages",
        MediaFolder.ItemImages => "ItemImages",
        _ => throw new ArgumentOutOfRangeException(nameof(folder), folder, null),
    };
}
