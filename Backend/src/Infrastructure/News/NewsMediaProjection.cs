using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.NewsEvents;
using SciFiLarpNetwork.Backend.Infrastructure.News.NewsDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.News;

/// <summary>
/// <see cref="NewsMediaReferenced"/> changes nothing shown; it only moves the stream version, so a removal decided
/// before a story started using the media conflicts.
/// </summary>
public partial class NewsMediaProjection : SingleStreamProjection<NewsMediaDocument, Guid>
{
    public NewsMediaProjection()
    {
        IncludeType<NewsMediaUploaded>();
        IncludeType<NewsMediaReferenced>();
        IncludeType<NewsMediaRemoved>();
    }

    public override NewsMediaDocument? Evolve(NewsMediaDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            NewsMediaUploaded uploaded => new NewsMediaDocument(
                uploaded.MediaId,
                uploaded.Name,
                uploaded.MediaType,
                uploaded.Kind,
                uploaded.ByteCount,
                uploaded.Width,
                uploaded.Height,
                uploaded.OccurredAtUtc,
                e.UserName,
                IsRemoved: false),
            NewsMediaRemoved when snapshot is not null => snapshot with { IsRemoved = true },
            _ => snapshot
        };
}
