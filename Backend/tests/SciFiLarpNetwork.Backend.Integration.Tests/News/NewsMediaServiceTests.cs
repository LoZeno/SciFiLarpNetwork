using SciFiLarpNetwork.Backend.Application.News;
using SciFiLarpNetwork.Backend.Domain.Events.NewsEvents;
using SciFiLarpNetwork.Backend.Domain.News;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;

namespace SciFiLarpNetwork.Backend.Tests.Integration.News;

[Collection("Backend database integration")]
public sealed class NewsMediaServiceTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private readonly NewsTestData _data = new(factory);

    private INewsMediaService Media => _data.Media;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task UploadedPictureHasItsTypeAndSize()
    {
        var uploaded = await _data.UploadAsync(NewsTestData.Picture(size: 3000, width: 800, height: 600), "  Harbour at dawn ");

        Assert.Equivalent(
            new NewsMediaSummary(uploaded.MediaId, "Harbour at dawn", "image/png", NewsMediaKind.Picture, 3000, 800, 600, uploaded.UploadedAtUtc, null),
            uploaded,
            strict: true);
    }

    [Fact]
    public async Task UploadedVideoHasItsType()
    {
        var uploaded = await _data.UploadVideoAsync();

        Assert.Equal(new { Type = "video/mp4", Kind = NewsMediaKind.Video }, new { Type = uploaded.MediaType, uploaded.Kind });
    }

    [Fact]
    public async Task StoredFileHoldsTheUploadedBytes()
    {
        var picture = NewsTestData.Picture();
        var uploaded = await _data.UploadAsync(picture);

        var content = await Media.OpenMediaAsync(uploaded.MediaId, Token);

        await using var stream = content!.Content;
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy, Token);
        Assert.Equal(picture, copy.ToArray());
    }

    [Fact]
    public async Task LibraryListsNewestUploadFirst()
    {
        var first = await _data.UploadPictureAsync("First");
        factory.Clock.Advance(TimeSpan.FromMinutes(1));
        var second = await _data.UploadVideoAsync("Second");

        var media = await Media.GetMediaAsync(Token);

        Assert.Equal([second.MediaId, first.MediaId], media.Select(item => item.MediaId));
    }

    [Fact]
    public async Task SvgIsRefusedAndNoFileIsKept()
    {
        var result = await Upload(NewsTestData.Text("<svg xmlns=\"http://www.w3.org/2000/svg\"/>"));

        Assert.Equal(new { Outcome = NewsOutcome.InvalidMediaFile, Files = 0 }, new { result.Outcome, Files = StoredFileCount() });
    }

    [Fact]
    public async Task PictureOverFiveMegabytesIsRefused()
    {
        var result = await Upload(NewsTestData.Picture(size: (5 * 1024 * 1024) + 1));

        Assert.Equal(NewsOutcome.MediaTooLarge, result.Outcome);
    }

    [Fact]
    public async Task VideoOverOneHundredMegabytesIsRefusedAndNoFileIsKept()
    {
        await using var video = new GeneratedStream(NewsTestData.Mp4Header, (100L * 1024 * 1024) + 1);

        var result = await Media.UploadMediaAsync("Too long", video, Token);

        Assert.Equal(new { Outcome = NewsOutcome.MediaTooLarge, Files = 0 }, new { result.Outcome, Files = StoredFileCount() });
    }

    [Fact]
    public async Task PictureOverTheDimensionLimitIsRefused()
    {
        var result = await Upload(NewsTestData.Picture(width: 4097, height: 100));

        Assert.Equal(NewsOutcome.MediaDimensionsTooLarge, result.Outcome);
    }

    [Fact]
    public async Task BlankNameIsRefused()
    {
        using var stream = new MemoryStream(NewsTestData.Picture());

        var result = await Media.UploadMediaAsync(" ", stream, Token);

        Assert.Equal(NewsOutcome.InvalidMediaName, result.Outcome);
    }

    [Fact]
    public async Task CancellationRightAfterTheUploadCommitsKeepsTheFile()
    {
        using var cancellation = new CancellationTokenSource();
        using var stream = new MemoryStream(NewsTestData.Picture());
        factory.NextCommit.ArmAfter<NewsMediaUploaded>(cancellation.Cancel);

        await Record.ExceptionAsync(() => Media.UploadMediaAsync("Harbour", stream, cancellation.Token));

        var uploaded = (await Media.GetMediaAsync(Token)).Single();
        Assert.NotNull(await Media.OpenMediaAsync(uploaded.MediaId, Token));
    }

    [Fact]
    public async Task UnusedMediaIsRemovedWithItsFile()
    {
        var uploaded = await _data.UploadPictureAsync();

        var outcome = await Media.RemoveMediaAsync(uploaded.MediaId, Token);

        Assert.Equal(
            new { Outcome = NewsOutcome.Succeeded, Listed = 0, Files = 0 },
            new { Outcome = outcome, Listed = (await Media.GetMediaAsync(Token)).Count, Files = StoredFileCount() });
    }

    [Fact]
    public async Task RemovedMediaCannotBeOpened()
    {
        var uploaded = await _data.UploadPictureAsync();
        await Media.RemoveMediaAsync(uploaded.MediaId, Token);

        Assert.Null(await Media.OpenMediaAsync(uploaded.MediaId, Token));
    }

    [Fact]
    public async Task RemovingUnknownMediaIsNotFound() =>
        Assert.Equal(NewsOutcome.NotFound, await Media.RemoveMediaAsync(Guid.NewGuid(), Token));

    [Fact]
    public async Task MediaAStoryUsesCannotBeRemoved()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        var picture = await _data.UploadPictureAsync();
        await _data.CreateAsync(_data.Content(network, author, NewsTestData.Embed(picture)));

        var outcome = await Media.RemoveMediaAsync(picture.MediaId, Token);

        Assert.Equal(new { Outcome = NewsOutcome.MediaInUse, Files = 1 }, new { Outcome = outcome, Files = StoredFileCount() });
    }

    [Fact]
    public async Task MediaCanBeRemovedOnceNoStoryUsesItAnyMore()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        var picture = await _data.UploadPictureAsync();
        var content = _data.Content(network, author, NewsTestData.Embed(picture));
        var story = await _data.CreateAsync(content);
        await _data.Stories.EditStoryAsync(story.StoryId, story.Version, content with { Body = "No picture now." }, Token);

        Assert.Equal(NewsOutcome.Succeeded, await Media.RemoveMediaAsync(picture.MediaId, Token));
    }

    [Fact]
    public async Task StoryCannotUseRemovedMedia()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        var picture = await _data.UploadPictureAsync();
        await Media.RemoveMediaAsync(picture.MediaId, Token);

        var result = await _data.Stories.CreateStoryAsync(Guid.NewGuid(), _data.Content(network, author, NewsTestData.Embed(picture)), Token);

        Assert.Equal(NewsOutcome.UnknownMedia, result.Outcome);
    }

    [Fact]
    public async Task ConcurrentRemovalAndFirstUseNeverLeaveAStoryShowingRemovedMedia()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        var picture = await _data.UploadPictureAsync();
        var content = _data.Content(network, author, NewsTestData.Embed(picture));

        factory.Clock.FirstReadBarrier = new Barrier(2);
        var create = _data.Stories.CreateStoryAsync(Guid.NewGuid(), content, Token);
        var remove = Media.RemoveMediaAsync(picture.MediaId, Token);
        await Task.WhenAll(create, remove);

        Assert.Equal(
            [NewsOutcome.Succeeded, NewsOutcome.ConcurrentChange],
            new[] { (await create).Outcome, await remove }.Order());
    }

    private async Task<NewsResult<NewsMediaSummary>> Upload(byte[] content)
    {
        using var stream = new MemoryStream(content);
        return await Media.UploadMediaAsync("Upload", stream, Token);
    }

    [Fact]
    public async Task UploadedFileIsStoredInTheNewsFolderUnderItsIdentifier()
    {
        var picture = NewsTestData.Picture();
        var uploaded = await _data.UploadAsync(picture);

        var stored = await File.ReadAllBytesAsync(Path.Combine(NewsFolder, uploaded.MediaId.ToString("D")), Token);

        Assert.Equal(picture, stored);
    }

    [Fact]
    public async Task FailedUploadEventLeavesNoFile()
    {
        using var stream = new MemoryStream(NewsTestData.Picture());
        factory.NextCommit.FailNextSaveOf<NewsMediaUploaded>();

        await Record.ExceptionAsync(() => Media.UploadMediaAsync("Harbour", stream, Token));

        Assert.Equal(0, StoredFileCount());
    }

    [Fact]
    public async Task UnusableMediaFolderReportsItsOwnError()
    {
        using var stream = new MemoryStream(NewsTestData.Picture());

        var exception = await factory.WithUnusableMediaFolderAsync(() => Media.UploadMediaAsync("Harbour", stream, Token));

        Assert.Equal(typeof(IOException), exception?.GetType());
    }

    private string NewsFolder => Path.Combine(factory.MediaRootPath, "News");

    private int StoredFileCount() => Directory.Exists(NewsFolder) ? Directory.GetFiles(NewsFolder).Length : 0;

    /// <summary>A read-only stream of <paramref name="length"/> bytes that starts with a header, without holding them.</summary>
    private sealed class GeneratedStream(byte[] header, long length) : Stream
    {
        private long _position;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => length;

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = (int)Math.Min(count, length - _position);
            for (var index = 0; index < read; index++)
            {
                var position = _position + index;
                buffer[offset + index] = position < header.Length ? header[position] : (byte)(position % 251);
            }

            _position += read;
            return read;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
