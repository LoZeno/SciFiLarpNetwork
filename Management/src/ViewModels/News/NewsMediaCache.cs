using SciFiLarpNetwork.Management.Api;

namespace SciFiLarpNetwork.Management.ViewModels.News;

/// <summary>
/// The library media the news screen and its dialogs have seen, with the pictures already downloaded. A media
/// identifier always names the same bytes, so a downloaded picture never needs to be fetched again.
/// </summary>
internal sealed class NewsMediaCache(IManagementApiClient apiClient, string apiBaseUrl)
{
    private readonly Dictionary<Guid, NewsMediaItem> _items = [];
    private readonly HashSet<Guid> _downloading = [];
    private readonly HashSet<Guid> _failed = [];

    public NewsMediaItem? Find(Guid mediaId) => _items.GetValueOrDefault(mediaId);

    /// <summary>Remembers media, keeping any picture already downloaded for it.</summary>
    public void Remember(IEnumerable<NewsMediaItem> items)
    {
        foreach (var item in items)
        {
            _items[item.MediaId] = _items.TryGetValue(item.MediaId, out var known) && known.PictureContent is { } content
                ? item with { PictureContent = content }
                : item;
        }
    }

    /// <summary>
    /// Downloads the remembered pictures among <paramref name="mediaIds"/> that are not downloaded yet. A picture
    /// already downloading is not asked for again, and one that could not be downloaded is not retried while the
    /// screen is open, so typing in the story text does not repeat requests; the preview shows its text instead.
    /// No cancellation token is passed, so a <see cref="TaskCanceledException"/> can only be the client's timeout.
    /// </summary>
    /// <returns>Whether any picture was downloaded.</returns>
    public async Task<bool> LoadPicturesAsync(IEnumerable<Guid> mediaIds, string locale)
    {
        var loaded = false;
        foreach (var item in mediaIds.Distinct().Select(Find).OfType<NewsMediaItem>()
                     .Where(item => item is { IsPicture: true, PictureContent: null, ContentHref: not null } &&
                                    !_failed.Contains(item.MediaId))
                     .ToArray())
        {
            if (!_downloading.Add(item.MediaId))
            {
                continue;
            }

            try
            {
                var content = await apiClient.GetImageAsync(apiBaseUrl, item.ContentHref!, locale);
                _items[item.MediaId] = item with { PictureContent = content };
                loaded = true;
            }
            catch (Exception failure) when (failure is HttpRequestException or InvalidOperationException or
                                                ManagementApiException or TaskCanceledException)
            {
                _failed.Add(item.MediaId);
                System.Diagnostics.Trace.TraceWarning("A news picture could not be downloaded: {0}", failure);
            }
            finally
            {
                _downloading.Remove(item.MediaId);
            }
        }

        return loaded;
    }
}
