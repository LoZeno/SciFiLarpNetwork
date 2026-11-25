using SciFiLarpNetwork.Common.NewsMarkdown;

namespace SciFiLarpNetwork.Management.ViewModels.News;

/// <summary>The media chosen in the library to place in a story, with its text and play settings.</summary>
public sealed record NewsMediaChoice(NewsMediaItem Media, NewsMediaEmbed Embed);
