namespace SciFiLarpNetwork.Common.NewsMarkdown;

/// <summary>A picture or video placed in a news body with <c>![text](media:&lt;id&gt;?options)</c>.</summary>
/// <param name="Text">The text shown to people who cannot see the media.</param>
public sealed record NewsMediaEmbed(Guid MediaId, string Text, NewsVideoOptions Options);
