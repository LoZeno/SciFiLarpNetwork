using System.Globalization;
using System.Net;
using System.Text.Json;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.News;

/// <summary>In-memory networks, characters, stories, and media that answer with the backend's Siren shapes.</summary>
internal sealed class FakeNewsApi : IManagementApiClient
{
    public const string NewsHref = "/api/v1/news";
    public const string NetworksHref = "/api/v1/networks";
    public const string AllNetworksHref = "/api/v1/networks?includeDisabled=true";
    public const string CharactersHref = "/api/v1/characters";
    public const string MediaHref = "/api/v1/news-media";
    public static readonly Guid CoreId = new("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid OuterId = new("aaaaaaaa-0000-0000-0000-000000000002");
    public static readonly Guid OldId = new("aaaaaaaa-0000-0000-0000-000000000003");
    public static readonly Guid KiraId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid OdoId = new("22222222-2222-2222-2222-222222222222");
    public static readonly DateTimeOffset Now = new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);
    public static readonly byte[] PictureBytes = [0x89, 0x50, 0x4E, 0x47, 1, 2, 3];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] PictureTypes = ["image/png", "image/jpeg", "image/webp", "image/gif"];
    private static readonly string[] VideoTypes = ["video/webm", "video/mp4"];

    public bool CanWrite { get; init; } = true;

    public List<FakeStory> Stories { get; } = [];

    public List<FakeMedia> Media { get; } = [];

    public List<(Guid Id, string Name, bool IsEnabled, bool IsDefault)> Networks { get; } =
    [
        (CoreId, "Core", true, true),
        (OuterId, "Outer", true, false),
        (OldId, "Old", false, false)
    ];

    public Dictionary<Guid, (int Number, string Name)> Characters { get; } = new()
    {
        [KiraId] = (1, "Kira"),
        [OdoId] = (2, "Odo")
    };

    public List<Submission> Submissions { get; } = [];

    /// <summary>Every list address the screen loaded, in order.</summary>
    public List<string> ListLoads { get; } = [];

    /// <summary>Every picture address downloaded, in order.</summary>
    public List<string> ImageDownloads { get; } = [];

    /// <summary>Every upload: the name field and the file name.</summary>
    public List<(string Name, string FileName)> Uploads { get; } = [];

    /// <summary>One-shot failures for the next submission of the named action.</summary>
    public Dictionary<string, Exception> Failures { get; } = [];

    public FakeStory AddStory(string title, Guid networkId, DateTimeOffset startsAt, DateTimeOffset endsAt, string body = "Story text.")
    {
        var story = new FakeStory(Guid.NewGuid(), networkId, KiraId, title, body, startsAt, endsAt, 2, 1);
        Stories.Add(story);
        return story;
    }

    public FakeMedia AddMedia(string name, bool isVideo)
    {
        var media = new FakeMedia(Guid.NewGuid(), name, isVideo ? "video/mp4" : "image/png", isVideo, isVideo ? 3 * 1024 * 1024 : 2048,
            Now.AddMinutes(Media.Count));
        Media.Add(media);
        return media;
    }

    public static string StoryHref(Guid storyId) => $"{NewsHref}/{storyId:D}";

    public static string ContentHref(Guid mediaId) => $"{MediaHref}/{mediaId:D}/content";

    public Task<SirenEntity> GetResourceAsync(string apiBaseUrl, string href, string locale, CancellationToken cancellationToken = default)
    {
        var (path, query) = Split(href);
        return Task.FromResult(path switch
        {
            NewsHref => ListLoaded(href, StoryCollection(Parameters(query))),
            NetworksHref => NetworkCollection(query.Contains("includeDisabled", StringComparison.Ordinal)),
            CharactersHref => CharacterCollection(Parameters(query)),
            MediaHref => MediaCollection(),
            _ => Stories.FirstOrDefault(story => StoryHref(story.Id) == path) is { } story
                ? Details(story)
                : throw new ManagementApiException("errors.notFound", HttpStatusCode.NotFound)
        });
    }

    public Task<SirenEntity> SubmitActionAsync(string apiBaseUrl, SirenAction action, JsonElement? body, string locale,
        CancellationToken cancellationToken = default)
    {
        Submissions.Add(new Submission(action.Name, body?.GetRawText()));
        if (Failures.Remove(action.Name, out var failure))
        {
            return Task.FromException<SirenEntity>(failure);
        }

        var request = body!.Value;
        var networkId = request.GetProperty("networkId").GetGuid();
        var authorId = request.GetProperty("authorId").GetGuid();
        var title = request.GetProperty("title").GetString()!;
        var text = request.GetProperty("body").GetString()!;
        var startsAt = request.GetProperty("startsAt").GetDateTimeOffset();
        var endsAt = request.GetProperty("endsAt").GetDateTimeOffset();
        var difficulty = request.GetProperty("hackingDifficulty").GetInt32();
        if (action.Name == "create-news-story")
        {
            var created = new FakeStory(request.GetProperty("storyId").GetGuid(), networkId, authorId, title, text, startsAt, endsAt, difficulty, 1);
            Stories.Add(created);
            return Task.FromResult(Details(created));
        }

        var index = Stories.FindIndex(story => StoryHref(story.Id) == action.Href);
        var edited = Stories[index] with
        {
            NetworkId = networkId,
            AuthorId = authorId,
            Title = title,
            Body = text,
            StartsAt = startsAt,
            EndsAt = endsAt,
            HackingDifficulty = difficulty,
            Version = Stories[index].Version + 1
        };
        Stories[index] = edited;
        return Task.FromResult(Details(edited));
    }

    public Task<SirenEntity> UploadFileActionAsync(string apiBaseUrl, SirenAction action, IReadOnlyDictionary<string, string> fields,
        UploadFile file, string locale, CancellationToken cancellationToken = default)
    {
        Uploads.Add((fields["name"], file.FileName));
        var media = AddMedia(fields["name"], file.FileName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(new SirenEntity
        {
            Class = ["news-media"],
            Properties = Json(MediaProperties(media)),
            Links = [new SirenLink { Rel = [SirenRelations.MediaContent], Href = ContentHref(media.Id) }]
        });
    }

    public Task ExecuteActionAsync(string apiBaseUrl, SirenAction action, string locale, CancellationToken cancellationToken = default)
    {
        Submissions.Add(new Submission(action.Name, null));
        var media = Media.Single(item => $"{MediaHref}/{item.Id:D}" == action.Href);
        if (Stories.Any(story => story.Body.Contains(media.Id.ToString("D"), StringComparison.Ordinal)))
        {
            return Task.FromException(new ManagementApiException("errors.mediaInUse", HttpStatusCode.Conflict));
        }

        Media.Remove(media);
        return Task.CompletedTask;
    }

    public Task<byte[]> GetImageAsync(string apiBaseUrl, string href, string locale, CancellationToken cancellationToken = default)
    {
        ImageDownloads.Add(href);
        return Task.FromResult(PictureBytes);
    }

    public Task<byte[]> GetFileAsync(string apiBaseUrl, string href, string mediaType, string locale,
        CancellationToken cancellationToken = default) =>
        Task.FromException<byte[]>(new NotSupportedException("This fake serves no files."));

    public Task<byte[]> DownloadActionAsync(string apiBaseUrl, SirenAction action, JsonElement body, string mediaType, string locale,
        CancellationToken cancellationToken = default) =>
        Task.FromException<byte[]>(new NotSupportedException("This fake serves no files."));

    public Task<ManagementSession> SignInAsync(string apiBaseUrl, string userName, string password, string locale,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ManagementSession(userName, "administrator", DateTimeOffset.UtcNow.AddHours(1)));

    public Task SignOutAsync(string apiBaseUrl, string locale, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<ManagementSession> RenewSessionAsync(string apiBaseUrl, string locale, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ManagementSession("operator", "administrator", DateTimeOffset.UtcNow.AddHours(1)));

    public Task<ApiDiscovery> DiscoverAsync(string apiBaseUrl, string locale, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ApiDiscovery(
            new SirenEntity { Class = ["api"], Properties = Json(new { title = "Administration" }) },
            [new SirenLink { Rel = [SirenRelations.Navigation, SirenRelations.News], Href = NewsHref, Title = "News" }]));

    private SirenEntity ListLoaded(string href, SirenEntity collection)
    {
        ListLoads.Add(href);
        return collection;
    }

    /// <summary>One network's stories, latest start first, 50 per page, optionally only those active at a time.</summary>
    private SirenEntity StoryCollection(Dictionary<string, string> parameters)
    {
        Guid? networkId = parameters.TryGetValue("networkId", out var id) ? Guid.Parse(id) : null;
        DateTimeOffset? activeAt = parameters.TryGetValue("activeAt", out var at)
            ? DateTimeOffset.Parse(at, CultureInfo.InvariantCulture)
            : null;
        var page = parameters.TryGetValue("page", out var number) ? int.Parse(number, CultureInfo.InvariantCulture) : 1;
        var listed = networkId is null
            ? []
            : Stories
                .Where(story => story.NetworkId == networkId && (activeAt is not { } time || (story.StartsAt <= time && time < story.EndsAt)))
                .OrderByDescending(story => story.StartsAt)
                .ToArray();
        var pageCount = Math.Max(1, (listed.Length + 49) / 50);
        var self = PageHref(networkId, activeAt, page);
        var links = new List<SirenLink>
        {
            new() { Rel = ["self"], Href = self },
            new() { Rel = [SirenRelations.Networks], Href = NetworksHref },
            new() { Rel = [SirenRelations.Characters], Href = CharactersHref },
            new() { Rel = [SirenRelations.NewsMedia], Href = MediaHref }
        };
        if (page > 1)
        {
            links.Add(new SirenLink { Rel = [SirenRelations.Previous], Href = PageHref(networkId, activeAt, page - 1) });
        }

        if (page < pageCount)
        {
            links.Add(new SirenLink { Rel = [SirenRelations.Next], Href = PageHref(networkId, activeAt, page + 1) });
        }

        return new SirenEntity
        {
            Class = ["news-collection"],
            Properties = Json(new
            {
                title = "News",
                description = "Network news",
                networkId,
                activeAt,
                pageNumber = page,
                pageCount,
                totalCount = listed.Length,
                maximumTitleLength = 128,
                maximumBodyLength = 10000,
                maximumMediaPerStory = 10,
                minimumHackingDifficulty = 1
            }),
            Entities = listed.Skip((page - 1) * 50).Take(50).Select(story => new SirenSubEntity
            {
                Rel = ["item"],
                Class = ["news-story"],
                Href = StoryHref(story.Id),
                Properties = Json(new
                {
                    storyId = story.Id,
                    networkId = story.NetworkId,
                    author = Character(story.AuthorId),
                    title = story.Title,
                    startsAtUtc = story.StartsAt,
                    endsAtUtc = story.EndsAt,
                    status = Status(story)
                })
            }).ToArray(),
            Links = links,
            Actions =
            [
                new SirenAction
                {
                    Name = "filter-news",
                    Href = NewsHref,
                    Method = "GET",
                    Fields = [new SirenField { Name = "networkId", Type = "hidden" }, new SirenField { Name = "activeAt", Type = "datetime-local" }]
                },
                .. CanWrite
                    ? [new SirenAction { Name = "create-news-story", Href = NewsHref, Method = "POST", Type = "application/json" }]
                    : Array.Empty<SirenAction>()
            ]
        };
    }

    private SirenEntity Details(FakeStory story)
    {
        var network = Networks.FirstOrDefault(candidate => candidate.Id == story.NetworkId);
        var media = Media.Where(item => story.Body.Contains(item.Id.ToString("D"), StringComparison.Ordinal)).ToArray();
        return new SirenEntity
        {
            Class = ["news-story"],
            Properties = Json(new
            {
                storyId = story.Id,
                network = new { networkId = story.NetworkId, isKnown = network.Name is not null, name = network.Name, isEnabled = network.IsEnabled },
                author = Character(story.AuthorId),
                title = story.Title,
                body = story.Body,
                startsAtUtc = story.StartsAt,
                endsAtUtc = story.EndsAt,
                hackingDifficulty = story.HackingDifficulty,
                status = Status(story),
                media = media.Select(MediaProperties).ToArray(),
                savedAtUtc = Now,
                savedBy = "local-admin",
                version = story.Version
            }),
            Entities = media.Select(MediaEntity).ToArray(),
            Links =
            [
                new SirenLink { Rel = ["self"], Href = StoryHref(story.Id) },
                new SirenLink { Rel = [SirenRelations.NewsMedia], Href = MediaHref }
            ],
            Actions = CanWrite
                ? [new SirenAction { Name = "edit-news-story", Href = StoryHref(story.Id), Method = "PUT", Type = "application/json" }]
                : null
        };
    }

    private SirenEntity MediaCollection() =>
        new()
        {
            Class = ["news-media-collection"],
            Properties = Json(new
            {
                title = "News media",
                description = "Pictures and videos",
                maximumPictureByteCount = 5L * 1024 * 1024,
                maximumVideoByteCount = 100L * 1024 * 1024,
                maximumPixelDimension = 4096,
                maximumNameLength = 128,
                pictureMediaTypes = PictureTypes,
                videoMediaTypes = VideoTypes
            }),
            Entities = Media.OrderByDescending(item => item.UploadedAt).Select(MediaEntity).ToArray(),
            Links = [new SirenLink { Rel = ["self"], Href = MediaHref }],
            Actions = CanWrite
                ? [new SirenAction { Name = "upload-news-media", Href = MediaHref, Method = "POST", Type = "multipart/form-data" }]
                : null
        };

    private SirenSubEntity MediaEntity(FakeMedia media) =>
        new()
        {
            Rel = ["item"],
            Class = ["news-media"],
            Properties = Json(MediaProperties(media)),
            Links = [new SirenLink { Rel = [SirenRelations.MediaContent], Href = ContentHref(media.Id) }],
            Actions = CanWrite
                ? [new SirenAction { Name = "remove-news-media", Href = $"{MediaHref}/{media.Id:D}", Method = "DELETE" }]
                : null
        };

    private static object MediaProperties(FakeMedia media) =>
        new
        {
            mediaId = media.Id,
            name = media.Name,
            mediaType = media.MediaType,
            kind = media.IsVideo ? "video" : "picture",
            byteCount = media.ByteCount,
            width = media.IsVideo ? (int?)null : 640,
            height = media.IsVideo ? (int?)null : 480,
            uploadedAtUtc = media.UploadedAt,
            uploadedBy = "local-admin"
        };

    private SirenEntity NetworkCollection(bool includeDisabled) =>
        new()
        {
            Class = ["network-collection"],
            Properties = Json(new { title = "Networks" }),
            Entities = Networks.Where(network => includeDisabled || network.IsEnabled).Select(network => new SirenSubEntity
            {
                Rel = ["item"],
                Class = ["network"],
                Properties = Json(new
                {
                    networkId = network.Id,
                    code = network.Name.ToLowerInvariant(),
                    name = network.Name,
                    theme = "blue",
                    isEnabled = network.IsEnabled,
                    isDefault = network.IsDefault,
                    version = 1
                })
            }).ToArray(),
            Links =
            [
                new SirenLink { Rel = ["self"], Href = includeDisabled ? AllNetworksHref : NetworksHref },
                new SirenLink { Rel = [SirenRelations.WithDisabled], Href = AllNetworksHref }
            ]
        };

    private SirenEntity CharacterCollection(Dictionary<string, string> parameters)
    {
        var search = parameters.GetValueOrDefault("search", string.Empty);
        return new SirenEntity
        {
            Class = ["character-collection"],
            Properties = Json(new { title = "Characters" }),
            Entities = Characters
                .Where(character => search.Length > 0 && character.Value.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .Select(character => new SirenSubEntity
                {
                    Rel = ["item"],
                    Properties = Json(new
                    {
                        characterId = character.Key,
                        number = character.Value.Number,
                        playerId = Guid.Empty,
                        playerDisplayName = "Ann",
                        name = character.Value.Name,
                        experiencePoints = 0,
                        status = "active",
                        qrIdentifier = "qr",
                        stats = Array.Empty<object>(),
                        isArchived = false,
                        version = 1
                    })
                })
                .ToArray(),
            Actions =
            [
                new SirenAction
                {
                    Name = "search-characters",
                    Href = CharactersHref,
                    Method = "GET",
                    Fields = [new SirenField { Name = "search" }]
                }
            ]
        };
    }

    private object Character(Guid characterId) =>
        Characters.TryGetValue(characterId, out var character)
            ? new { characterId, isKnown = true, number = (int?)character.Number, name = (string?)character.Name, isArchived = false }
            : new { characterId, isKnown = false, number = (int?)null, name = (string?)null, isArchived = false };

    private static string Status(FakeStory story) =>
        Now < story.StartsAt ? "scheduled" : Now < story.EndsAt ? "active" : "expired";

    private static string PageHref(Guid? networkId, DateTimeOffset? activeAt, int page)
    {
        var parameters = new List<string>();
        if (networkId is { } network)
        {
            parameters.Add($"networkId={network:D}");
        }

        if (activeAt is { } time)
        {
            parameters.Add($"activeAt={Uri.EscapeDataString(time.ToString("O", CultureInfo.InvariantCulture))}");
        }

        if (page > 1)
        {
            parameters.Add($"page={page}");
        }

        return parameters.Count == 0 ? NewsHref : $"{NewsHref}?{string.Join('&', parameters)}";
    }

    private static Dictionary<string, string> Parameters(string query) =>
        query.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(pair => pair[0], pair => Uri.UnescapeDataString(pair[1]));

    private static (string Path, string Query) Split(string href)
    {
        var index = href.IndexOf('?', StringComparison.Ordinal);
        return index < 0 ? (href, string.Empty) : (href[..index], href[(index + 1)..]);
    }

    private static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value, JsonOptions);

    public sealed record Submission(string ActionName, string? Body);
}
