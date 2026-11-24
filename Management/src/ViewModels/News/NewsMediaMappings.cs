using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Api.News;
using SciFiLarpNetwork.Management.Api.Skills;

namespace SciFiLarpNetwork.Management.ViewModels.News;

internal static class NewsMediaMappings
{
    public static NewsMediaItem ToMediaItem(SirenSubEntity entity) =>
        ToMediaItem(SirenProperties.Read<NewsMediaProperties>(entity.Properties),
            entity.FindLinkHref(SirenRelations.MediaContent), entity.FindAction("remove-news-media"));

    public static NewsMediaItem ToMediaItem(SirenEntity entity) =>
        ToMediaItem(SirenProperties.Read<NewsMediaProperties>(entity.Properties),
            entity.FindLinkHref(SirenRelations.MediaContent), entity.FindAction("remove-news-media"));

    private static NewsMediaItem ToMediaItem(NewsMediaProperties media, string? contentHref, SirenAction? remove) =>
        new(media.MediaId, media.Name, media.MediaType, media.IsVideo, media.ByteCount, media.Width, media.Height,
            contentHref, remove);
}
