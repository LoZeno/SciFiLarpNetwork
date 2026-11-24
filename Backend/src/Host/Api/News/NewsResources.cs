using SciFiLarpNetwork.Backend.Application.News;
using SciFiLarpNetwork.Backend.Domain.News;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.News;

/// <summary>
/// Builds the Siren representations of news stories and the media library. Write actions are advertised only when
/// <paramref name="canWrite"/> is set; the server still authorizes and validates every request.
/// </summary>
internal sealed class NewsResources(IApplicationText text, string pathBase, bool canWrite)
{
    private const string JsonType = "application/json";

    public string NewsPath => $"{pathBase}/api/v1/news";

    public string StoryPath(Guid storyId) => $"{NewsPath}/{storyId:D}";

    public string MediaPath => $"{pathBase}/api/v1/news-media";

    public string MediaItemPath(Guid mediaId) => $"{MediaPath}/{mediaId:D}";

    public string MediaContentPath(Guid mediaId) => $"{MediaItemPath(mediaId)}/content";

    private string NetworksPath => $"{pathBase}/api/v1/networks";

    private string CharactersPath => $"{pathBase}/api/v1/characters";

    public SirenEntity<NewsCollectionProperties> NewsCollection(NewsStoryPage page, NewsStoryQuery query) =>
        new()
        {
            Class = ["news-collection"],
            Properties = new NewsCollectionProperties(
                text.GetLocalizedText("api.news"),
                text.GetLocalizedText("api.newsDescription"),
                query.NetworkId,
                query.ActiveAt,
                page.PageNumber,
                page.PageCount,
                page.TotalCount,
                NewsTexts.MaximumTitleLength,
                NewsTexts.MaximumBodyLength,
                NewsTexts.MaximumMediaPerStory,
                NewsTexts.MinimumHackingDifficulty),
            Entities = page.Items.Select(story => new SirenSubEntity
            {
                Rel = ["item"],
                Class = ["news-story"],
                Href = StoryPath(story.StoryId),
                Properties = SirenJson.ToElement(story)
            }).ToArray(),
            Links = PageLinks(page, query),
            Actions =
            [
                new SirenAction
                {
                    Name = "filter-news",
                    Href = NewsPath,
                    Method = "GET",
                    Title = text.GetLocalizedText("api.filterNews"),
                    Fields =
                    [
                        Field("networkId", "hidden", "news.network", query.NetworkId),
                        Field("activeAt", "datetime-local", "news.activeAt", query.ActiveAt)
                    ]
                },
                .. canWrite
                    ?
                    [
                        new SirenAction
                        {
                            Name = "create-news-story",
                            Href = NewsPath,
                            Method = "POST",
                            Type = JsonType,
                            Title = text.GetLocalizedText("api.createNewsStory"),
                            Fields = [Field("storyId", "hidden", "news.storyId", (Guid?)null), .. ContentFields(null)]
                        }
                    ]
                    : Array.Empty<SirenAction>()
            ]
        };

    public SirenEntity<NewsStoryDetails> Story(NewsStoryDetails story) =>
        new()
        {
            Class = ["news-story"],
            Properties = story,
            Entities = story.Media.Select(MediaEntity).ToArray(),
            Links =
            [
                new SirenLink { Rel = ["self"], Href = StoryPath(story.StoryId) },
                new SirenLink { Rel = ["collection"], Href = $"{NewsPath}?networkId={story.Network.NetworkId:D}" },
                new SirenLink { Rel = [SirenRelations.Network], Href = $"{NetworksPath}/{story.Network.NetworkId:D}" },
                new SirenLink { Rel = [SirenRelations.NewsMedia], Href = MediaPath }
            ],
            Actions = canWrite
                ?
                [
                    new SirenAction
                    {
                        Name = "edit-news-story",
                        Href = StoryPath(story.StoryId),
                        Method = "PUT",
                        Type = JsonType,
                        Title = text.GetLocalizedText("api.editNewsStory"),
                        Fields =
                        [
                            new SirenField
                            {
                                Name = "version",
                                Type = "hidden",
                                Value = SirenJson.ToElement(story.Version),
                                Title = text.GetLocalizedText("skills.version")
                            },
                            .. ContentFields(story)
                        ]
                    }
                ]
                : null
        };

    public SirenEntity<NewsMediaCollectionProperties> MediaCollection(IReadOnlyList<NewsMediaSummary> media) =>
        new()
        {
            Class = ["news-media-collection"],
            Properties = new NewsMediaCollectionProperties(
                text.GetLocalizedText("api.newsMedia"),
                text.GetLocalizedText("api.newsMediaDescription"),
                NewsMediaFormats.MaximumPictureByteCount,
                NewsMediaFormats.MaximumVideoByteCount,
                NewsMediaFormats.MaximumPixelDimension,
                NewsTexts.MaximumMediaNameLength,
                NewsLimits.PictureMediaTypes,
                NewsLimits.VideoMediaTypes),
            Entities = media.Select(MediaEntity).ToArray(),
            Links =
            [
                new SirenLink { Rel = ["self"], Href = MediaPath },
                new SirenLink { Rel = [SirenRelations.News], Href = NewsPath }
            ],
            Actions = canWrite
                ?
                [
                    new SirenAction
                    {
                        Name = "upload-news-media",
                        Href = MediaPath,
                        Method = "POST",
                        Type = "multipart/form-data",
                        Title = text.GetLocalizedText("api.uploadNewsMedia"),
                        Fields =
                        [
                            Field("name", "text", "news.mediaName", (string?)null),
                            Field("file", "file", "news.mediaFile", (string?)null)
                        ]
                    }
                ]
                : null
        };

    public SirenEntity<NewsMediaSummary> MediaItem(NewsMediaSummary media) =>
        new()
        {
            Class = ["news-media"],
            Properties = media,
            Links =
            [
                new SirenLink { Rel = ["self"], Href = MediaItemPath(media.MediaId) },
                new SirenLink { Rel = ["collection"], Href = MediaPath },
                ContentLink(media)
            ],
            Actions = canWrite ? [RemoveAction(media)] : null
        };

    private SirenSubEntity MediaEntity(NewsMediaSummary media) =>
        new()
        {
            Rel = ["item"],
            Class = ["news-media"],
            Properties = SirenJson.ToElement(media),
            Links = [new SirenLink { Rel = ["self"], Href = MediaItemPath(media.MediaId) }, ContentLink(media)],
            Actions = canWrite ? [RemoveAction(media)] : null
        };

    private SirenLink ContentLink(NewsMediaSummary media) =>
        new() { Rel = [SirenRelations.MediaContent], Href = MediaContentPath(media.MediaId), Type = media.MediaType };

    private SirenAction RemoveAction(NewsMediaSummary media) =>
        new()
        {
            Name = "remove-news-media",
            Href = MediaItemPath(media.MediaId),
            Method = "DELETE",
            Title = text.GetLocalizedText("api.removeNewsMedia")
        };

    private SirenField[] ContentFields(NewsStoryDetails? story) =>
    [
        Field("networkId", "hidden", "news.network", story?.Network.NetworkId),
        Field("authorId", "hidden", "news.author", story?.Author.CharacterId),
        Field("title", "text", "news.title", story?.Title),
        Field("body", "textarea", "news.body", story?.Body),
        Field("startsAt", "datetime-local", "news.startsAt", story?.StartsAtUtc),
        Field("endsAt", "datetime-local", "news.endsAt", story?.EndsAtUtc),
        Field("hackingDifficulty", "number", "news.hackingDifficulty",
            story?.HackingDifficulty ?? NewsTexts.MinimumHackingDifficulty)
    ];

    private SirenLink[] PageLinks(NewsStoryPage page, NewsStoryQuery query)
    {
        var links = new List<SirenLink>
        {
            new() { Rel = ["self"], Href = PageHref(query) },
            new() { Rel = [SirenRelations.First], Href = PageHref(query with { PageNumber = 1 }) },
            new() { Rel = [SirenRelations.Last], Href = PageHref(query with { PageNumber = page.PageCount }) },
            new() { Rel = [SirenRelations.Networks], Href = NetworksPath },
            new() { Rel = [SirenRelations.Characters], Href = CharactersPath },
            new() { Rel = [SirenRelations.NewsMedia], Href = MediaPath }
        };
        if (page.PageNumber > 1)
        {
            links.Add(new SirenLink
            {
                Rel = [SirenRelations.Previous],
                Href = PageHref(query with { PageNumber = Math.Min(page.PageNumber - 1, page.PageCount) })
            });
        }

        if (page.PageNumber < page.PageCount)
        {
            links.Add(new SirenLink { Rel = [SirenRelations.Next], Href = PageHref(query with { PageNumber = page.PageNumber + 1 }) });
        }

        return [.. links];
    }

    private string PageHref(NewsStoryQuery query)
    {
        var parameters = new List<string>();
        if (query.NetworkId is { } networkId)
        {
            parameters.Add($"networkId={networkId:D}");
        }

        if (query.ActiveAt is { } activeAt)
        {
            parameters.Add($"activeAt={Uri.EscapeDataString(activeAt.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture))}");
        }

        if (query.PageNumber > 1)
        {
            parameters.Add($"page={query.PageNumber}");
        }

        return parameters.Count == 0 ? NewsPath : $"{NewsPath}?{string.Join('&', parameters)}";
    }

    private SirenField Field<T>(string name, string type, string titleKey, T? value) =>
        new()
        {
            Name = name,
            Type = type,
            Title = text.GetLocalizedText(titleKey),
            Value = value is null ? null : SirenJson.ToElement(value)
        };
}
