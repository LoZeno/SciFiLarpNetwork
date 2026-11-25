namespace SciFiLarpNetwork.Management.ViewModels.News.Preview;

/// <summary>A library video, shown by its name, type, size, and play settings; the app does not play videos.</summary>
/// <param name="Text">The text for people who cannot see the video.</param>
/// <param name="Description">The name, type, size, and play settings.</param>
public sealed record NewsPreviewVideo(Guid MediaId, string Text, string Description, int Indent) : NewsPreviewBlock(Indent);
