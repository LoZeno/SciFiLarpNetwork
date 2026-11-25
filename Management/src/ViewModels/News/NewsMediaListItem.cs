namespace SciFiLarpNetwork.Management.ViewModels.News;

/// <param name="DisplayName">The name, kind, type, and size.</param>
public sealed record NewsMediaListItem(NewsMediaItem Media, string DisplayName);
