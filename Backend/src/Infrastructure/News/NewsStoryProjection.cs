using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.NewsEvents;
using SciFiLarpNetwork.Backend.Infrastructure.News.NewsDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.News;

/// <summary>Creation and every edit carry all the story's values, so each replaces the document.</summary>
public partial class NewsStoryProjection : SingleStreamProjection<NewsStoryDocument, Guid>
{
    public NewsStoryProjection()
    {
        IncludeType<NewsStoryCreated>();
        IncludeType<NewsStoryEdited>();
    }

    public override NewsStoryDocument? Evolve(NewsStoryDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            NewsStoryCreated created => new NewsStoryDocument(
                created.StoryId,
                created.NetworkId,
                created.AuthorId,
                created.Title,
                created.Body,
                created.StartsAtUtc,
                created.EndsAtUtc,
                created.HackingDifficulty,
                [.. created.MediaIds],
                created.OccurredAtUtc,
                e.UserName),
            NewsStoryEdited edited when snapshot is not null => snapshot with
            {
                NetworkId = edited.NetworkId,
                AuthorId = edited.AuthorId,
                Title = edited.Title,
                Body = edited.Body,
                StartsAtUtc = edited.StartsAtUtc,
                EndsAtUtc = edited.EndsAtUtc,
                HackingDifficulty = edited.HackingDifficulty,
                MediaIds = [.. edited.MediaIds],
                SavedAtUtc = edited.OccurredAtUtc,
                SavedBy = e.UserName
            },
            _ => snapshot
        };
}
