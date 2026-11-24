using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Tests.Integration.News;

[Collection("Backend database integration")]
public sealed class NewsApiTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private const string NewsPath = "/local/api/v1/news";
    private const string MediaPath = "/local/api/v1/news-media";

    private readonly NewsTestData _data = new(factory);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task VersionRootAdvertisesNewsToReaders()
    {
        using var client = factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await GetAsync(client, token, "/local/api/v1");

        Assert.Equal(NewsPath, (await ReadAsync(response)).LinkHref(SirenRelations.News));
    }

    [Fact]
    public async Task AnonymousRequestCannotListNews()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(NewsPath, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReaderGetsOnlyTheFilterAction()
    {
        using var client = factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await GetAsync(client, token, NewsPath);

        Assert.Equal(["filter-news"], (await ReadAsync(response)).ActionNames());
    }

    [Fact]
    public async Task AdministratorGetsTheFilterAndCreateActions()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await GetAsync(client, token, NewsPath);

        Assert.Equal(["filter-news", "create-news-story"], (await ReadAsync(response)).ActionNames());
    }

    [Fact]
    public async Task CollectionCarriesTheStoryLimits()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await GetAsync(client, token, NewsPath);

        var document = await ReadAsync(response);
        Assert.Equal(
            new { Title = 128, Body = 10000, Media = 10, Difficulty = 1 },
            new
            {
                Title = document.Property("maximumTitleLength").GetInt32(),
                Body = document.Property("maximumBodyLength").GetInt32(),
                Media = document.Property("maximumMediaPerStory").GetInt32(),
                Difficulty = document.Property("minimumHackingDifficulty").GetInt32()
            });
    }

    [Fact]
    public async Task AdministratorCreatesAStoryAndIsNamedAsSaver()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await PostAsync(client, token, NewsPath, await StoryAsync());

        Assert.Equal(
            new { Status = HttpStatusCode.Created, SavedBy = (string?)"local-admin" },
            new { Status = response.StatusCode, SavedBy = (await ReadAsync(response)).Property("savedBy").GetString() });
    }

    [Fact]
    public async Task ReaderCannotCreateStories()
    {
        using var client = factory.CreateClient();
        var (_, readerToken) = await BothTokensAsync(client);

        using var response = await PostAsync(client, readerToken, NewsPath, await StoryAsync());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReaderSeesAStoryWithoutTheEditAction()
    {
        using var client = factory.CreateClient();
        var (adminToken, readerToken) = await BothTokensAsync(client);
        using var created = await PostAsync(client, adminToken, NewsPath, await StoryAsync());
        var storyPath = (await ReadAsync(created)).LinkHref("self");

        using var response = await GetAsync(client, readerToken, storyPath);

        Assert.Empty((await ReadAsync(response)).ActionNames());
    }

    [Fact]
    public async Task AdministratorSeesTheEditActionWithTheVersion()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        using var created = await PostAsync(client, token, NewsPath, await StoryAsync());

        Assert.Equal("1", (await ReadAsync(created)).ActionFieldRawValue("edit-news-story", "version"));
    }

    [Fact]
    public async Task InvalidTimesReturnTheirStableCode()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var story = await StoryAsync();

        using var response = await PostAsync(client, token, NewsPath, story with { endsAt = story.startsAt });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.invalidNewsTimes"), await ErrorAsync(response));
    }

    [Fact]
    public async Task StaleEditReturnsTheConcurrentChangeCode()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var story = await StoryAsync();
        using var created = await PostAsync(client, token, NewsPath, story);
        var storyPath = (await ReadAsync(created)).LinkHref("self");
        (await PutAsync(client, token, storyPath, Edit(story, 1))).Dispose();

        using var response = await PutAsync(client, token, storyPath, Edit(story, 1));

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.Conflict, "errors.concurrentChange"), await ErrorAsync(response));
    }

    [Fact]
    public async Task UnknownStoryIsNotFound()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await GetAsync(client, token, $"{NewsPath}/{Guid.NewGuid():D}");

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.NotFound, "errors.notFound"), await ErrorAsync(response));
    }

    [Fact]
    public async Task ActiveAtFilterAcceptsAnInstantWithItsOffset()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var story = await StoryAsync();
        (await PostAsync(client, token, NewsPath, story)).Dispose();
        var later = Uri.EscapeDataString(story.endsAt.ToOffset(TimeSpan.FromHours(2)).ToString("O"));

        using var response = await GetAsync(client, token, $"{NewsPath}?networkId={story.networkId:D}&activeAt={later}");

        Assert.Equal(0, (await ReadAsync(response)).Property("totalCount").GetInt64());
    }

    [Fact]
    public async Task MediaCollectionCarriesTheUploadLimits()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await GetAsync(client, token, MediaPath);

        var document = await ReadAsync(response);
        Assert.Equal(
            new { Picture = 5L * 1024 * 1024, Video = 100L * 1024 * 1024, Pixels = 4096 },
            new
            {
                Picture = document.Property("maximumPictureByteCount").GetInt64(),
                Video = document.Property("maximumVideoByteCount").GetInt64(),
                Pixels = document.Property("maximumPixelDimension").GetInt32()
            });
    }

    [Fact]
    public async Task ReaderGetsNoMediaActions()
    {
        using var client = factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await GetAsync(client, token, MediaPath);

        Assert.Empty((await ReadAsync(response)).ActionNames());
    }

    [Fact]
    public async Task AdministratorGetsTheUploadAction()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await GetAsync(client, token, MediaPath);

        Assert.Equal(["upload-news-media"], (await ReadAsync(response)).ActionNames());
    }

    [Fact]
    public async Task UploadedPictureIsServedWithItsTypeAndNoSniffing()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var picture = NewsTestData.Picture();
        using var uploaded = await UploadAsync(client, token, "Harbour", picture);
        var contentPath = (await ReadAsync(uploaded)).LinkHref(SirenRelations.MediaContent);

        using var response = await GetAsync(client, token, contentPath);

        Assert.Equal(
            new { Status = HttpStatusCode.OK, Type = (string?)"image/png", NoSniff = "nosniff", Bytes = Convert.ToHexString(picture) },
            new
            {
                Status = response.StatusCode,
                Type = response.Content.Headers.ContentType?.MediaType,
                NoSniff = string.Join(',', response.Headers.GetValues("X-Content-Type-Options")),
                Bytes = Convert.ToHexString(await response.Content.ReadAsByteArrayAsync(Token))
            });
    }

    [Fact]
    public async Task VideoCanBeRequestedInParts()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var video = NewsTestData.Video();
        using var uploaded = await UploadAsync(client, token, "Launch", video);
        var contentPath = (await ReadAsync(uploaded)).LinkHref(SirenRelations.MediaContent);
        using var request = new HttpRequestMessage(HttpMethod.Get, contentPath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Range = new RangeHeaderValue(100, 199);

        using var response = await client.SendAsync(request, Token);

        Assert.Equal(
            new { Status = HttpStatusCode.PartialContent, Bytes = Convert.ToHexString(video[100..200]) },
            new { Status = response.StatusCode, Bytes = Convert.ToHexString(await response.Content.ReadAsByteArrayAsync(Token)) });
    }

    [Fact]
    public async Task ReaderCannotUploadMedia()
    {
        using var client = factory.CreateClient();
        var (_, readerToken) = await BothTokensAsync(client);

        using var response = await UploadAsync(client, readerToken, "Harbour", NewsTestData.Picture());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UnsupportedFileReturnsItsStableCode()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await UploadAsync(client, token, "Logo", NewsTestData.Text("<svg xmlns=\"http://www.w3.org/2000/svg\"/>"));

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.invalidMediaFile"), await ErrorAsync(response));
    }

    [Fact]
    public async Task UsedMediaCannotBeRemoved()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        using var uploaded = await UploadAsync(client, token, "Harbour", NewsTestData.Picture());
        var media = await ReadAsync(uploaded);
        var story = await StoryAsync();
        var embed = $"![Harbour](media:{media.Property("mediaId").GetGuid():D})";
        (await PostAsync(client, token, NewsPath, story with { body = embed })).Dispose();

        using var response = await SendAsync(client, token, HttpMethod.Delete, media.LinkHref("self"));

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.Conflict, "errors.mediaInUse"), await ErrorAsync(response));
    }

    [Fact]
    public async Task UnusedMediaIsRemoved()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        using var uploaded = await UploadAsync(client, token, "Harbour", NewsTestData.Picture());
        var media = await ReadAsync(uploaded);

        using var response = await SendAsync(client, token, HttpMethod.Delete, media.LinkHref("self"));

        using var content = await GetAsync(client, token, media.LinkHref(SirenRelations.MediaContent));
        Assert.Equal(
            new { Removed = HttpStatusCode.NoContent, Content = HttpStatusCode.NotFound },
            new { Removed = response.StatusCode, Content = content.StatusCode });
    }

    private async Task<StoryRequest> StoryAsync()
    {
        var network = await _data.CreateNetworkAsync();
        var author = await _data.CreateCharacterAsync();
        var now = factory.Clock.GetUtcNow();
        return new StoryRequest(Guid.NewGuid(), network.NetworkId, author.CharacterId, "Fleet arrives", "The fleet arrives **tonight**.",
            now.AddHours(-1), now.AddHours(1), 2);
    }

    private static EditRequest Edit(StoryRequest story, long version) =>
        new(version, story.networkId, story.authorId, "Fleet delayed", story.body, story.startsAt, story.endsAt, story.hackingDifficulty);

    private async Task<string> AdministratorTokenAsync(HttpClient client)
    {
        await factory.BootstrapAdministratorAsync();
        return await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");
    }

    private async Task<(string AdministratorToken, string ReaderToken)> BothTokensAsync(HttpClient client)
    {
        await factory.CreateReaderAccountAsync(client);
        return (await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1"),
            await AdministratorAuthenticationFactory.SignInReaderAsync(client));
    }

    private async Task<string> ReaderTokenAsync(HttpClient client)
    {
        await factory.CreateReaderAccountAsync(client);
        return await AdministratorAuthenticationFactory.SignInReaderAsync(client);
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, string token, string name, byte[] file)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(name), "name");
        var content = new ByteArrayContent(file);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(content, "file", "upload.bin");
        using var request = new HttpRequestMessage(HttpMethod.Post, MediaPath) { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd(SirenMediaTypes.Json);
        return await client.SendAsync(request, Token);
    }

    private static Task<HttpResponseMessage> GetAsync(HttpClient client, string token, string path) =>
        SendAsync(client, token, HttpMethod.Get, path);

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string token, HttpMethod method, string path) =>
        AdministratorAuthenticationFactory.SendAuthorizedAsync(client, method, path, token);

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string token, string path, object body) =>
        AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, path, token, body);

    private static Task<HttpResponseMessage> PutAsync(HttpClient client, string token, string path, object body) =>
        AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Put, path, token, body);

    private static async Task<SirenDocument> ReadAsync(HttpResponseMessage response) =>
        new(JsonDocument.Parse(await response.Content.ReadAsStringAsync(Token)).RootElement.Clone());

    private static async Task<ErrorSnapshot> ErrorAsync(HttpResponseMessage response) =>
        new(response.StatusCode, (await ReadAsync(response)).Property("code").GetString());

    // Lower-case members serialize to the request's JSON field names.
#pragma warning disable IDE1006
    private sealed record StoryRequest(
        Guid storyId,
        Guid networkId,
        Guid authorId,
        string title,
        string body,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        int hackingDifficulty);

    private sealed record EditRequest(
        long version,
        Guid networkId,
        Guid authorId,
        string title,
        string body,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        int hackingDifficulty);
#pragma warning restore IDE1006

    private sealed record ErrorSnapshot(HttpStatusCode Status, string? Code);

    private sealed record SirenDocument(JsonElement Root)
    {
        public JsonElement Property(string name) => Root.GetProperty("properties").GetProperty(name);

        public string LinkHref(string rel) =>
            Root.GetProperty("links").EnumerateArray()
                .Where(link => link.GetProperty("rel").EnumerateArray().Any(value => value.GetString() == rel))
                .Select(link => link.GetProperty("href").GetString())
                .FirstOrDefault() ?? throw new InvalidOperationException($"No {rel} link.");

        public string?[] ActionNames() =>
            Root.TryGetProperty("actions", out var actions)
                ? actions.EnumerateArray().Select(action => action.GetProperty("name").GetString()).ToArray()
                : [];

        public string ActionFieldRawValue(string action, string field) =>
            Root.GetProperty("actions").EnumerateArray()
                .Single(candidate => candidate.GetProperty("name").GetString() == action)
                .GetProperty("fields").EnumerateArray()
                .Single(candidate => candidate.GetProperty("name").GetString() == field)
                .GetProperty("value").GetRawText();
    }
}
