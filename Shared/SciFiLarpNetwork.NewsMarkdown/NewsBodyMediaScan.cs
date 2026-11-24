namespace SciFiLarpNetwork.Common.NewsMarkdown;

/// <param name="Embeds">The body's pictures and videos in document order; empty when <paramref name="Problem"/> is set.</param>
public sealed record NewsBodyMediaScan(IReadOnlyList<NewsMediaEmbed> Embeds, NewsBodyMediaProblem? Problem)
{
    public static NewsBodyMediaScan Failed(NewsBodyMediaProblem problem) => new([], problem);
}
