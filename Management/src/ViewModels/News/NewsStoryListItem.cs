namespace SciFiLarpNetwork.Management.ViewModels.News;

/// <param name="DisplayName">The start, end, title, author, and status.</param>
public sealed record NewsStoryListItem(Guid StoryId, string Href, string DisplayName);
