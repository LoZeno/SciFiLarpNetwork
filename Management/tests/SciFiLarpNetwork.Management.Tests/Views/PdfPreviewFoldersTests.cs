using SciFiLarpNetwork.Management.Views.Players;

namespace SciFiLarpNetwork.Management.Tests.Views;

public sealed class PdfPreviewFoldersTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("scifilarp-preview-tests-");

    public void Dispose()
    {
        _root.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void NewPreviewDeletesPreviewsOlderThanADay()
    {
        var folders = new PdfPreviewFolders(_root.FullName);
        var old = folders.Create();
        Directory.SetLastWriteTimeUtc(old.FullName, DateTime.UtcNow.AddDays(-2));

        folders.Create();

        Assert.False(Directory.Exists(old.FullName));
    }

    [Fact]
    public void NewPreviewKeepsRecentPreviews()
    {
        var folders = new PdfPreviewFolders(_root.FullName);
        var recent = folders.Create();

        folders.Create();

        Assert.True(Directory.Exists(recent.FullName));
    }

    [Fact]
    public void NewPreviewKeepsOtherFolders()
    {
        var other = _root.CreateSubdirectory("someone-else");
        Directory.SetLastWriteTimeUtc(other.FullName, DateTime.UtcNow.AddDays(-2));

        new PdfPreviewFolders(_root.FullName).Create();

        Assert.True(Directory.Exists(other.FullName));
    }
}
