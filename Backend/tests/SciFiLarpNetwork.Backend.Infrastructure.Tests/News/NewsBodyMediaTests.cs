using SciFiLarpNetwork.Common.NewsMarkdown;

namespace SciFiLarpNetwork.Backend.Tests.Infrastructure.News;

public sealed class NewsBodyMediaTests
{
    private const string Harbour = "6f1c2d6e-1b3a-4a51-9d21-3c1f6a6f0b01";
    private const string Launch = "8a2e4b10-7c55-4e0f-8f0e-2d4a7b9c1e02";

    private static readonly Guid HarbourId = Guid.Parse(Harbour);
    private static readonly Guid LaunchId = Guid.Parse(Launch);

    [Fact]
    public void BodyWithoutImagesHasNoEmbeds() =>
        Assert.Empty(NewsBodyMedia.Scan("# Fleet\n\n**Tonight** at the dock.\n\n- one\n- two").Embeds);

    [Fact]
    public void BodyWithoutImagesReportsNoProblem() =>
        Assert.Null(NewsBodyMedia.Scan("# Fleet\n\n**Tonight** at the dock.").Problem);

    [Fact]
    public void EmbedsAreFoundInDocumentOrderWithTheirTextAndOptions() =>
        Assert.Equal(
            [
                new NewsMediaEmbed(HarbourId, "Harbour at dawn", NewsVideoOptions.None),
                new NewsMediaEmbed(LaunchId, "Launch", NewsVideoOptions.Autoplay | NewsVideoOptions.Loop | NewsVideoOptions.Controls)
            ],
            NewsBodyMedia.Scan($"Intro\n\n![Harbour at dawn](media:{Harbour})\n\n- ![Launch](media:{Launch}?loop&controls&autoplay)").Embeds);

    [Fact]
    public void FormattedTextIsReadAsPlainText() =>
        Assert.Equal("The big harbour", NewsBodyMedia.Scan($"![The *big* harbour](media:{Harbour})").Embeds.Single().Text);

    [Fact]
    public void ReferenceStyleImagesAreChecked() =>
        Assert.Equal(
            NewsBodyMediaProblem.ExternalImage,
            NewsBodyMedia.Scan("![Harbour][pic]\n\n[pic]: http://example.com/harbour.png").Problem);

    [Theory]
    [InlineData("![Harbour](http://example.com/harbour.png)")]
    [InlineData("![Harbour](/api/v1/news-media/x/content)")]
    [InlineData("![Harbour](data:image/png;base64,AAAA)")]
    [InlineData("![Harbour](MEDIA:6f1c2d6e-1b3a-4a51-9d21-3c1f6a6f0b01)")]
    [InlineData("![Harbour]()")]
    public void ImagesOutsideTheLibraryAreRefused(string body) =>
        Assert.Equal(NewsBodyMediaProblem.ExternalImage, NewsBodyMedia.Scan(body).Problem);

    [Theory]
    [InlineData("![Harbour](media:not-a-guid)")]
    [InlineData("![Harbour](media:00000000-0000-0000-0000-000000000000)")]
    [InlineData("![Harbour](media:6f1c2d6e-1b3a-4a51-9d21-3c1f6a6f0b01?mute)")]
    [InlineData("![Harbour](media:6f1c2d6e-1b3a-4a51-9d21-3c1f6a6f0b01?loop&loop)")]
    [InlineData("![Harbour](media:6f1c2d6e-1b3a-4a51-9d21-3c1f6a6f0b01?)")]
    [InlineData("![Harbour](media:6f1c2d6e1b3a4a519d213c1f6a6f0b01)")]
    public void MalformedLibraryAddressesAreRefused(string body) =>
        Assert.Equal(NewsBodyMediaProblem.InvalidReference, NewsBodyMedia.Scan(body).Problem);

    [Theory]
    [InlineData("![](media:6f1c2d6e-1b3a-4a51-9d21-3c1f6a6f0b01)")]
    [InlineData("![  ](media:6f1c2d6e-1b3a-4a51-9d21-3c1f6a6f0b01)")]
    public void MediaWithoutTextIsRefused(string body) =>
        Assert.Equal(NewsBodyMediaProblem.MissingText, NewsBodyMedia.Scan(body).Problem);

    [Fact]
    public void RawHtmlImagesAndVideosAreTextSoNoProblem() =>
        Assert.Null(NewsBodyMedia.Scan("<img src=\"http://example.com/x.png\"> <video src=\"x.mp4\" autoplay></video>").Problem);

    [Fact]
    public void RawHtmlImagesAreNotEmbeds() =>
        Assert.Empty(NewsBodyMedia.Scan("<img src=\"http://example.com/x.png\">").Embeds);

    [Fact]
    public void ImagesInsideCodeAreText() =>
        Assert.Null(NewsBodyMedia.Scan("`![Harbour](http://example.com/x.png)`").Problem);

    [Fact]
    public void WrittenEmbedReadsBackTheSame()
    {
        var embed = new NewsMediaEmbed(LaunchId, "Launch [day] *one*", NewsVideoOptions.Autoplay | NewsVideoOptions.Controls);

        Assert.Equal([embed], NewsBodyMedia.Scan(NewsBodyMedia.Write(embed)).Embeds);
    }

    [Fact]
    public void WrittenPictureHasNoOptions() =>
        Assert.Equal($"![Harbour](media:{Harbour})", NewsBodyMedia.Write(new NewsMediaEmbed(HarbourId, "Harbour", NewsVideoOptions.None)));
}
