using Markdig;
using Markdig.Syntax;

namespace SciFiLarpNetwork.Common.NewsMarkdown;

/// <summary>
/// The one Markdown dialect of news bodies: CommonMark with raw HTML turned into text. The backend's checks and every
/// renderer parse with it, so an image the checks never saw cannot appear in a rendered story.
/// </summary>
public static class NewsMarkdown
{
    public static MarkdownPipeline Pipeline { get; } = new MarkdownPipelineBuilder().DisableHtml().Build();

    public static MarkdownDocument Parse(string body) => Markdown.Parse(body, Pipeline);
}
