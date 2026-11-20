using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.CardTemplates;
using SciFiLarpNetwork.Backend.Application.CharacterCards;
using SciFiLarpNetwork.Backend.Infrastructure.CharacterCards;

namespace SciFiLarpNetwork.Backend.Tests.Integration.CardTemplates;

/// <summary>
/// Runs templates through the bundled Typst to show what the sandbox lets a template do: read the card data, and
/// nothing else.
/// </summary>
public sealed class TemplateSandboxTests : IDisposable
{
    private static readonly CharacterCardDocument Sample = new("en", new Dictionary<string, string>(), SampleCharacterCards.Cards);
    private readonly ServiceProvider _provider;

    public TemplateSandboxTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCardPrinting(new ConfigurationManager());
        _provider = services.BuildServiceProvider();
    }

    private ICharacterCardRenderer Renderer => _provider.GetRequiredService<ICharacterCardRenderer>();

    public void Dispose() => _provider.Dispose();

    [Theory]
    [InlineData("#import \"@preview/cetz:0.3.0\": *")]
    [InlineData("#import \"@local/cards:0.1.0\": *")]
    [InlineData("#import \"@scifilarp/cards:0.1.0\": *")]
    [InlineData("#import (\"@pre\" + \"view/cetz:0.3.0\"): *")]
    [InlineData("#include \"@preview/cetz:0.3.0\"")]
    [InlineData("#read(\"other.txt\")")]
    [InlineData("#read(\"../cards.pdf\")")]
    [InlineData("#read(\"../no-certificate.pem\")")]
    [InlineData("#read(\"../../../../../../../../etc/hosts\")")]
    [InlineData("#read(\"/../cards.json\")")]
    [InlineData("#read(\"/etc/hosts\")")]
    [InlineData("#read(\"C:/Windows/win.ini\")")]
    [InlineData("#read(\"C:\\\\Windows\\\\win.ini\")")]
    [InlineData("#read(\"\\\\\\\\localhost\\\\c$\\\\Windows\\\\win.ini\")")]
    [InlineData("#read(\"file:///etc/hosts\")")]
    [InlineData("#read(\"<stdin>\")")]
    [InlineData("#image(\"x.png\")")]
    [InlineData("#image(\"../x.png\")")]
    [InlineData("#json(\"/etc/passwd\")")]
    [InlineData("#json(\"../data/../../cards.json\")")]
    [InlineData("#csv(\"people.csv\")")]
    [InlineData("#yaml(\"../cards.yaml\")")]
    [InlineData("#plugin(\"x.wasm\")")]
    [InlineData("#let broken =")]
    public async Task TemplateThatReachesOutsideTheCardDataIsRejected(string template) =>
        await Assert.ThrowsAsync<CardTemplateException>(() => RenderAsync(template));

    [Theory]
    [InlineData("#json(\"cards.json\").cards.len()")]
    [InlineData("#json(\"/cards.json\").cards.len()")]
    [InlineData("#json(\"./cards.json\").cards.len()")]
    public async Task TemplateCanReadTheCardData(string template) =>
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(await RenderAsync(template), 0, 4));

    [Fact]
    public async Task PackageImportFailsBeforeAnyDownloadStarts()
    {
        var failure = await Assert.ThrowsAsync<CardTemplateException>(() => RenderAsync("#import \"@preview/cetz:0.3.0\": *"));

        Assert.DoesNotContain("download", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TypstErrorDoesNotShowServerFolders()
    {
        var failure = await Assert.ThrowsAsync<CardTemplateException>(() => RenderAsync("#read(\"other.txt\")"));

        Assert.DoesNotContain("scifilarp-cards-", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TypstErrorNamesTheProblem()
    {
        var failure = await Assert.ThrowsAsync<CardTemplateException>(() => RenderAsync("#read(\"other.txt\")"));

        Assert.Contains("file not found", failure.Message, StringComparison.Ordinal);
    }

    private Task<byte[]> RenderAsync(string template) =>
        Renderer.RenderAsync(Sample, Encoding.UTF8.GetBytes(template), TestContext.Current.CancellationToken);
}
