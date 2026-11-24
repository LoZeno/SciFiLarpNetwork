namespace SciFiLarpNetwork.Management.ViewModels.News.Preview;

/// <summary>A run of story text with its emphasis.</summary>
public sealed record NewsPreviewInline(string Text, bool IsBold = false, bool IsItalic = false, bool IsCode = false);
