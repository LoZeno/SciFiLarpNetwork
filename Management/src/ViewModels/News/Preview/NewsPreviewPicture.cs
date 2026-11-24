namespace SciFiLarpNetwork.Management.ViewModels.News.Preview;

/// <summary>A library picture.</summary>
/// <param name="Text">The text for people who cannot see the picture.</param>
/// <param name="Content">The downloaded picture; <see langword="null"/> while it is not loaded.</param>
public sealed record NewsPreviewPicture(Guid MediaId, string Text, byte[]? Content, int Indent) : NewsPreviewBlock(Indent);
