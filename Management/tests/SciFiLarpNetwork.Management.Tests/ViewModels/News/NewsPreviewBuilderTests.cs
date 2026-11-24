using SciFiLarpNetwork.Management.ViewModels.News;
using SciFiLarpNetwork.Management.ViewModels.News.Preview;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.News;

public sealed class NewsPreviewBuilderTests
{
    private static readonly Guid PictureId = new("6f1c2d6e-1b3a-4a51-9d21-3c1f6a6f0b01");
    private static readonly Guid VideoId = new("8a2e4b10-7c55-4e0f-8f0e-2d4a7b9c1e02");
    private static readonly byte[] Picture = [1, 2, 3];

    private static readonly NewsPreviewBuilder Builder = new(
        id => id == PictureId ? new NewsMediaItem(PictureId, "Harbour", "image/png", false, 2048, 640, 480, null, null, Picture)
            : id == VideoId ? new NewsMediaItem(VideoId, "Launch", "video/webm", true, 5 * 1024 * 1024, null, null, null, null)
            : null,
        key => key);

    [Fact]
    public void HeadingsKeepTheirLevel() =>
        Assert.Equal([1, 3], Builder.Build("# One\n\n### Three").OfType<NewsPreviewText>().Select(text => text.HeadingLevel));

    [Fact]
    public void EmphasisIsKeptOnTheRuns() =>
        Assert.Equal(
            [new NewsPreviewInline("plain "), new NewsPreviewInline("bold", IsBold: true), new NewsPreviewInline(" "), new NewsPreviewInline("italic", IsItalic: true), new NewsPreviewInline(" "), new NewsPreviewInline("code", IsCode: true)],
            ((NewsPreviewText)Builder.Build("plain **bold** *italic* `code`").Single()).Inlines);

    [Fact]
    public void ListItemsGetTheirMarkersAndIndent() =>
        Assert.Equal(
            [("•", 1, "one"), ("•", 1, "two"), ("3.", 1, "three"), ("4.", 1, "four")],
            Builder.Build("- one\n- two\n\n3. three\n4. four").OfType<NewsPreviewText>().Select(text => (text.Marker, text.Indent, text.PlainText)));

    [Fact]
    public void QuotesAreIndented() =>
        Assert.Equal(1, Builder.Build("> quoted").Single().Indent);

    [Fact]
    public void RawHtmlStaysText() =>
        Assert.Equal("<script>alert(1)</script>", ((NewsPreviewText)Builder.Build("<script>alert(1)</script>").Single()).PlainText);

    [Fact]
    public void LinksStayText() =>
        Assert.Equal("see the dock", ((NewsPreviewText)Builder.Build("see [the dock](http://example.com)").Single()).PlainText);

    [Fact]
    public void PictureBecomesABlockOfItsOwnWithItsBytes() =>
        Assert.Equal(
            ["text:Before", "picture:Harbour at dawn", "text:after"],
            Builder.Build($"Before ![Harbour at dawn](media:{PictureId:D}) after").Select(Describe));

    [Fact]
    public void PictureCarriesItsDownloadedBytes() =>
        Assert.Same(Picture, Builder.Build($"![Harbour](media:{PictureId:D})").OfType<NewsPreviewPicture>().Single().Content);

    [Fact]
    public void VideoIsDescribedWithItsPlaySettings() =>
        Assert.Equal(
            "Launch · video/webm · 5 MB · news.autoplay · news.loop",
            Builder.Build($"![Launch](media:{VideoId:D}?loop&autoplay)").OfType<NewsPreviewVideo>().Single().Description);

    [Fact]
    public void ImageFromOutsideTheLibraryIsAProblem() =>
        Assert.Equal("errors.externalImage", Builder.Build("![Logo](http://example.com/logo.png)").OfType<NewsPreviewProblem>().Single().Message);

    [Fact]
    public void MediaNotInTheLibraryIsAProblem() =>
        Assert.Equal("errors.unknownMedia", Builder.Build($"![Gone](media:{Guid.NewGuid():D})").OfType<NewsPreviewProblem>().Single().Message);

    [Fact]
    public void ThematicBreakIsARule() =>
        Assert.IsType<NewsPreviewRule>(Builder.Build("---").Single());

    [Fact]
    public void CodeBlockKeepsItsLines() =>
        Assert.Equal("line one\nline two", ((NewsPreviewText)Builder.Build("```\nline one\nline two\n```").Single()).PlainText);

    private static string Describe(NewsPreviewBlock block) => block switch
    {
        NewsPreviewText text => $"text:{text.PlainText.Trim()}",
        NewsPreviewPicture picture => $"picture:{picture.Text}",
        _ => block.GetType().Name
    };
}
