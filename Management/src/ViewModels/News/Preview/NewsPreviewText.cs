namespace SciFiLarpNetwork.Management.ViewModels.News.Preview;

/// <summary>A paragraph, heading, list item, or code block.</summary>
/// <param name="HeadingLevel">1 to 6 for a heading; 0 for other text.</param>
/// <param name="Marker">The list marker, such as <c>•</c> or <c>2.</c>; empty outside lists.</param>
public sealed record NewsPreviewText(IReadOnlyList<NewsPreviewInline> Inlines, int HeadingLevel, string Marker, int Indent)
    : NewsPreviewBlock(Indent)
{
    public string PlainText => string.Concat(Inlines.Select(inline => inline.Text));

    public bool HasMarker => Marker.Length > 0;
}
