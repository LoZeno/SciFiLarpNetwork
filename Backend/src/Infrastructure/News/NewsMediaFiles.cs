namespace SciFiLarpNetwork.Backend.Infrastructure.News;

/// <summary>
/// The media folder. Each file is named by its media identifier only, so no uploaded name ever reaches a path. An
/// upload is written under a temporary name and given its final name only once its type is checked.
/// </summary>
internal sealed class NewsMediaFiles(string rootPath)
{
    private const int CopyBufferSize = 81920;

    /// <summary>
    /// Copies an upload to its temporary file, stopping once it exceeds <paramref name="maximumByteCount"/>.
    /// </summary>
    /// <returns>The byte count, or <see langword="null"/> when the upload is larger than allowed.</returns>
    public async Task<long?> WriteTemporaryAsync(Guid mediaId, Stream content, long maximumByteCount,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(rootPath);
        var buffer = new byte[CopyBufferSize];
        long total = 0;
        await using var file = new FileStream(TemporaryPath(mediaId), FileMode.CreateNew, FileAccess.Write,
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
    public async Task<byte[]> ReadTemporaryStartAsync(Guid mediaId, long count, CancellationToken cancellationToken)
    {
        await using var file = File.OpenRead(TemporaryPath(mediaId));
        var buffer = new byte[(int)Math.Min(count, file.Length)];
        await file.ReadExactlyAsync(buffer, cancellationToken);
        return buffer;
    }

    public void CommitTemporary(Guid mediaId) => File.Move(TemporaryPath(mediaId), FinalPath(mediaId));

    public void DeleteTemporary(Guid mediaId) => File.Delete(TemporaryPath(mediaId));

    public void Delete(Guid mediaId) => File.Delete(FinalPath(mediaId));

    /// <summary>Opens a stored file for reading, or returns <see langword="null"/> when it is missing.</summary>
    public Stream? Open(Guid mediaId)
    {
        try
        {
            return new FileStream(FinalPath(mediaId), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete,
                CopyBufferSize, useAsync: true);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }

    private string FinalPath(Guid mediaId) => Path.Combine(rootPath, mediaId.ToString("D"));

    private string TemporaryPath(Guid mediaId) => Path.Combine(rootPath, $"{mediaId:D}.upload");
}
