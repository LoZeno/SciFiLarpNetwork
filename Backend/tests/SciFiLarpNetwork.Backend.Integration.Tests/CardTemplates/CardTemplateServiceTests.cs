using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.CardTemplates;
using SciFiLarpNetwork.Backend.Application.CharacterCards;
using SciFiLarpNetwork.Backend.Domain.Events.CardTemplateEvents;
using SciFiLarpNetwork.Backend.Infrastructure.CardTemplates.CardTemplateDocuments;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;

namespace SciFiLarpNetwork.Backend.Tests.Integration.CardTemplates;

[Collection("Backend database integration")]
public sealed class CardTemplateServiceTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private static readonly CharacterCardDocument Sample = new("en", new Dictionary<string, string>(), SampleCharacterCards.Cards);
    private static readonly byte[] ValidTemplate = Encoding.UTF8.GetBytes("#let data = json(\"cards.json\")\nCustom template");
    private static readonly byte[] OtherValidTemplate = Encoding.UTF8.GetBytes("#let data = json(\"cards.json\")\nOther template");

    private ICharacterCardTemplateService Templates => factory.Services.GetRequiredService<ICharacterCardTemplateService>();

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task DefaultTemplateIsUsedBeforeAnyUpload()
    {
        var template = await Templates.GetTemplateAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new CardTemplateSummary(true, null, null, 0), template);
    }

    [Fact]
    public async Task DefaultSourceIsTheBundledTemplate()
    {
        var source = Encoding.UTF8.GetString(await Templates.GetSourceAsync(TestContext.Current.CancellationToken));

        Assert.StartsWith("// Default character card template.", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UploadedSourceIsStoredUnchanged()
    {
        await UploadAsync(0, ValidTemplate);

        Assert.Equal(ValidTemplate, await Templates.GetSourceAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UploadRecordsWhen()
    {
        var before = Microseconds(factory.Clock.GetUtcNow());
        var result = await UploadAsync(0, ValidTemplate);

        Assert.InRange(Microseconds(result.Value!.UploadedAtUtc!.Value), before, Microseconds(factory.Clock.GetUtcNow()));
    }

    [Fact]
    public async Task UploadedTemplateIsNotTheDefault()
    {
        var result = await UploadAsync(0, ValidTemplate);

        Assert.Equal(new { IsDefault = false, Version = 1L }, new { result.Value!.IsDefault, result.Value.Version });
    }

    [Fact]
    public async Task ResetUsesTheDefaultTemplateAgain()
    {
        await UploadAsync(0, ValidTemplate);
        await Templates.ResetAsync(1, TestContext.Current.CancellationToken);

        Assert.StartsWith("// Default character card template.",
            Encoding.UTF8.GetString(await Templates.GetSourceAsync(TestContext.Current.CancellationToken)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResetReportsTheDefaultTemplate()
    {
        await UploadAsync(0, ValidTemplate);

        var result = await Templates.ResetAsync(1, TestContext.Current.CancellationToken);

        Assert.Equal(new CardTemplateSummary(true, null, null, 2), result.Value);
    }

    [Fact]
    public async Task ResetOfTheDefaultTemplateChangesNothing()
    {
        var result = await Templates.ResetAsync(0, TestContext.Current.CancellationToken);

        Assert.Equal(new CardTemplateSummary(true, null, null, 0), result.Value);
    }

    [Fact]
    public async Task UploadAndResetAreRecordedAsEvents()
    {
        await UploadAsync(0, ValidTemplate);
        await Templates.ResetAsync(1, TestContext.Current.CancellationToken);

        var events = await factory.LoadStreamEventsAsync(CardTemplateDocument.CharacterStreamId);

        Assert.Equal([typeof(CardTemplateUploaded), typeof(CardTemplateReset)], events.Select(e => e.EventType));
    }

    [Fact]
    public async Task UploadEventRecordsTheSourceSize()
    {
        await UploadAsync(0, ValidTemplate);

        var events = await factory.LoadStreamEventsAsync(CardTemplateDocument.CharacterStreamId);

        Assert.Equal(ValidTemplate.Length, ((CardTemplateUploaded)events.Single().Data).ByteCount);
    }

    [Fact]
    public async Task TemplateAtTheSizeLimitIsAccepted()
    {
        var result = await UploadAsync(0, Padded(ICharacterCardTemplateService.MaximumByteCount));

        Assert.Equal(CardTemplateOutcome.Succeeded, result.Outcome);
    }

    [Fact]
    public async Task TemplateOverTheSizeLimitIsRejected()
    {
        var result = await UploadAsync(0, Padded(ICharacterCardTemplateService.MaximumByteCount + 1));

        Assert.Equal(CardTemplateOutcome.TemplateTooLarge, result.Outcome);
    }

    [Fact]
    public async Task PreviewOverTheSizeLimitIsRejected()
    {
        var preview = await Templates.PreviewAsync(Padded(ICharacterCardTemplateService.MaximumByteCount + 1), Sample,
            TestContext.Current.CancellationToken);

        Assert.Equal(CardTemplateOutcome.TemplateTooLarge, preview.Outcome);
    }

    [Theory]
    [InlineData("#import \"@preview/cetz:0.3.0\": *")]
    [InlineData("#read(\"other.txt\")")]
    [InlineData("#let broken =")]
    [InlineData("ÿ")]
    public async Task InvalidTemplateIsRejected(string template)
    {
        var result = await UploadAsync(0, Latin1OrUtf8(template));

        Assert.Equal(CardTemplateOutcome.InvalidTemplate, result.Outcome);
    }

    [Fact]
    public async Task InvalidTemplateReportsTypstError()
    {
        var result = await UploadAsync(0, Encoding.UTF8.GetBytes("#read(\"other.txt\")"));

        Assert.Contains("file not found", result.TemplateError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidTemplateIsNotSaved()
    {
        await UploadAsync(0, Encoding.UTF8.GetBytes("#let broken ="));

        Assert.Empty(await factory.LoadStreamEventsAsync(CardTemplateDocument.CharacterStreamId));
    }

    [Fact]
    public async Task UploadOnAStaleVersionIsRejected()
    {
        await UploadAsync(0, ValidTemplate);

        var result = await UploadAsync(0, OtherValidTemplate);

        Assert.Equal(CardTemplateOutcome.StaleVersion, result.Outcome);
    }

    [Fact]
    public async Task ResetOnAStaleVersionIsRejected()
    {
        await UploadAsync(0, ValidTemplate);
        await UploadAsync(1, OtherValidTemplate);

        var result = await Templates.ResetAsync(1, TestContext.Current.CancellationToken);

        Assert.Equal(CardTemplateOutcome.StaleVersion, result.Outcome);
    }

    [Fact]
    public async Task ConcurrentFirstUploadsSaveExactlyOne()
    {
        factory.Clock.FirstReadBarrier = new Barrier(2);
        var results = await Task.WhenAll(UploadAsync(0, ValidTemplate), UploadAsync(0, OtherValidTemplate));

        Assert.Equal(
            [CardTemplateOutcome.Succeeded, CardTemplateOutcome.StaleVersion],
            results.Select(result => result.Outcome).Order());
    }

    [Fact]
    public async Task ConcurrentLaterUploadsSaveExactlyOne()
    {
        await UploadAsync(0, ValidTemplate);

        factory.Clock.FirstReadBarrier = new Barrier(2);
        var results = await Task.WhenAll(UploadAsync(1, ValidTemplate), UploadAsync(1, OtherValidTemplate));

        Assert.Equal(
            [CardTemplateOutcome.Succeeded, CardTemplateOutcome.StaleVersion],
            results.Select(result => result.Outcome).Order());
    }

    [Fact]
    public async Task ConcurrentUploadAndResetSaveExactlyOne()
    {
        await UploadAsync(0, ValidTemplate);

        factory.Clock.FirstReadBarrier = new Barrier(2);
        var results = await Task.WhenAll(UploadAsync(1, OtherValidTemplate),
            Templates.ResetAsync(1, TestContext.Current.CancellationToken));

        Assert.Equal(
            [CardTemplateOutcome.Succeeded, CardTemplateOutcome.StaleVersion],
            results.Select(result => result.Outcome).Order());
    }

    private Task<CardTemplateResult> UploadAsync(long version, byte[] template) =>
        Templates.UploadAsync(version, template, Sample, TestContext.Current.CancellationToken);

    private static byte[] Padded(int byteCount)
    {
        var start = Encoding.UTF8.GetBytes("Padded template\n// ");
        return [.. start, .. Enumerable.Repeat((byte)'x', byteCount - start.Length)];
    }

    // Postgres keeps time to the microsecond.
    private static long Microseconds(DateTimeOffset time) => time.UtcTicks / 10;

    // A lone 0xFF byte is not UTF-8, which Typst requires.
    private static byte[] Latin1OrUtf8(string template) =>
        template == "ÿ" ? [0xFF] : Encoding.UTF8.GetBytes(template);
}
