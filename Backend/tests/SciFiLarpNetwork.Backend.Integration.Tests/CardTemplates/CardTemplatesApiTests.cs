using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;
using UglyToad.PdfPig;

namespace SciFiLarpNetwork.Backend.Tests.Integration.CardTemplates;

[Collection("Backend database integration")]
public sealed class CardTemplatesApiTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private const string TemplatePath = "/local/api/v1/card-templates/character";
    private const string PlayersPath = "/local/api/v1/players";

    private static readonly byte[] MarkerTemplate = Encoding.UTF8.GetBytes(
        "#let data = json(\"cards.json\")\n#for card in data.cards [UploadedMarker #card.characterName #pagebreak(weak: true)]");

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task ReaderSeesThatTheDefaultTemplateIsUsed()
    {
        using var client = factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, TemplatePath, token);

        Assert.True((await JsonAsync(response)).GetProperty("properties").GetProperty("isDefault").GetBoolean());
    }

    [Fact]
    public async Task CollectionListsTheCharacterCardTemplate()
    {
        using var client = factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get,
            "/local/api/v1/card-templates", token);

        Assert.Equal([TemplatePath],
            (await JsonAsync(response)).GetProperty("entities").EnumerateArray().Select(entity => entity.GetProperty("href").GetString()));
    }

    [Fact]
    public async Task UploadWhenTypstCannotRunReturnsItsStableCode()
    {
        using var brokenFactory = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("CardPrinting:TypstPath", Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}", "typst")));
        using var client = brokenFactory.CreateClient();
        await factory.BootstrapAdministratorAsync();
        var token = await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");

        using var response = await UploadAsync(client, token, 0, MarkerTemplate);

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.InternalServerError, "errors.cardRenderingFailed", false),
            await ErrorAsync(response, _ => false));
    }

    [Fact]
    public async Task PreviewWhenTypstCannotRunReturnsItsStableCode()
    {
        using var brokenFactory = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("CardPrinting:TypstPath", Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}", "typst")));
        using var client = brokenFactory.CreateClient();
        await factory.BootstrapAdministratorAsync();
        var token = await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");

        using var response = await SendPdfRequestAsync(client, token, TemplatePath + "/preview", new { content = MarkerTemplate });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.InternalServerError, "errors.cardRenderingFailed", false),
            await ErrorAsync(response, _ => false));
    }

    [Fact]
    public async Task ReaderCanDownloadTheTemplate()
    {
        using var client = factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await DownloadAsync(client, token);

        Assert.Equal(
            new { Status = HttpStatusCode.OK, MediaType = (string?)"text/plain" },
            new { Status = response.StatusCode, response.Content.Headers.ContentType?.MediaType });
    }

    [Fact]
    public async Task ReaderGetsNoChangeActions()
    {
        using var client = factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, TemplatePath, token);

        Assert.False((await JsonAsync(response)).TryGetProperty("actions", out _));
    }

    [Fact]
    public async Task ReaderSeesTheCardTemplatesScreen()
    {
        using var client = factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, "/local/api/v1", token);

        Assert.Contains("/local/api/v1/card-templates", LinkHrefs(await JsonAsync(response)));
    }

    [Theory]
    [InlineData("PUT", "")]
    [InlineData("POST", "/preview")]
    [InlineData("POST", "/reset")]
    public async Task ReaderCannotChangeOrPreviewTheTemplate(string method, string suffix)
    {
        using var client = factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, new HttpMethod(method),
            TemplatePath + suffix, token, new { version = 0, content = MarkerTemplate });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousRequestIsUnauthorized()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(TemplatePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdministratorCanUploadAndPreviewTheDefaultTemplate()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, TemplatePath, token);

        Assert.Equal(["upload-card-template", "preview-card-template"], ActionNames(await JsonAsync(response)));
    }

    [Fact]
    public async Task AdministratorCanResetAnUploadedTemplate()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await UploadAsync(client, token, 0, MarkerTemplate);

        Assert.Equal(["upload-card-template", "preview-card-template", "reset-card-template"], ActionNames(await JsonAsync(response)));
    }

    [Fact]
    public async Task UploadRecordsWhoUploaded()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await UploadAsync(client, token, 0, MarkerTemplate);

        Assert.Equal("local-admin", (await JsonAsync(response)).GetProperty("properties").GetProperty("uploadedBy").GetString());
    }

    [Fact]
    public async Task UploadedTemplateIsDownloaded()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        (await UploadAsync(client, token, 0, MarkerTemplate)).Dispose();

        using var response = await DownloadAsync(client, token);

        Assert.Equal(MarkerTemplate, await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UploadedTemplatePrintsCharacterCards()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        (await UploadAsync(client, token, 0, MarkerTemplate)).Dispose();
        var characterId = await CreateCharacterAsync(client, token);

        using var response = await SendPdfRequestAsync(client, token, "/local/api/v1/character-cards", new { characterIds = new[] { characterId } });

        Assert.Equal(["UploadedMarker", "Kira"], await PdfWordsAsync(response));
    }

    [Fact]
    public async Task ResetTemplatePrintsWithTheDefaultTemplateAgain()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        (await UploadAsync(client, token, 0, MarkerTemplate)).Dispose();
        (await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, TemplatePath + "/reset", token,
            new { version = 1 })).Dispose();
        var characterId = await CreateCharacterAsync(client, token);

        using var response = await SendPdfRequestAsync(client, token, "/local/api/v1/character-cards", new { characterIds = new[] { characterId } });

        Assert.DoesNotContain("UploadedMarker", await PdfWordsAsync(response));
    }

    [Fact]
    public async Task PreviewPrintsTheSampleCardsWithTheFile()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await SendPdfRequestAsync(client, token, TemplatePath + "/preview", new { content = MarkerTemplate });

        Assert.Equal(["UploadedMarker", "Nova", "Ardent"], (await PdfWordsAsync(response))[..3]);
    }

    [Fact]
    public async Task PreviewSavesNothing()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        (await SendPdfRequestAsync(client, token, TemplatePath + "/preview", new { content = MarkerTemplate })).Dispose();

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, TemplatePath, token);

        Assert.True((await JsonAsync(response)).GetProperty("properties").GetProperty("isDefault").GetBoolean());
    }

    [Fact]
    public async Task UploadWithAPackageImportIsRejectedWithTheTypstError()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await UploadAsync(client, token, 0, Encoding.UTF8.GetBytes("#import \"@preview/cetz:0.3.0\": *"));

        Assert.Equal(
            new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.invalidCardTemplate", true),
            await ErrorAsync(response, detail => detail.Contains("@preview/cetz:0.3.0", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task PreviewThatReadsAnotherFileIsRejectedWithTheTypstError()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await SendPdfRequestAsync(client, token, TemplatePath + "/preview",
            new { content = Encoding.UTF8.GetBytes("#read(\"../cards.pdf\")") });

        Assert.Equal(
            new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.invalidCardTemplate", true),
            await ErrorAsync(response, detail => detail.Contains("escape the project root", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("en", "The file is empty.")]
    [InlineData("it", "Il file è vuoto.")]
    public async Task UploadOfAnEmptyFileIsRejected(string language, string message)
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);

        using var response = await UploadAsync(client, token, 0, []);

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.invalidCardTemplate", true),
            await ErrorAsync(response, detail => detail == message));
    }

    [Fact]
    public async Task PreviewOfAnEmptyFileIsRejected()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await SendPdfRequestAsync(client, token, TemplatePath + "/preview", new { content = Array.Empty<byte>() });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.invalidCardTemplate", true),
            await ErrorAsync(response, detail => detail == "The file is empty."));
    }

    [Fact]
    public async Task ResetOnAStaleVersionIsAConflict()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        (await UploadAsync(client, token, 0, MarkerTemplate)).Dispose();

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post,
            TemplatePath + "/reset", token, new { version = 0 });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.Conflict, "errors.staleVersion", false), await ErrorAsync(response, _ => false));
    }

    [Fact]
    public async Task UploadWithoutContentIsAValidationError()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Put, TemplatePath,
            token, new { version = 0 });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.validation", false), await ErrorAsync(response, _ => false));
    }

    private async Task<string> AdministratorTokenAsync(HttpClient client)
    {
        await factory.BootstrapAdministratorAsync();
        return await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");
    }

    private async Task<string> ReaderTokenAsync(HttpClient client)
    {
        await factory.CreateReaderAccountAsync(client);
        return await AdministratorAuthenticationFactory.SignInReaderAsync(client);
    }

    private static Task<HttpResponseMessage> UploadAsync(HttpClient client, string token, long version, byte[] content) =>
        AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Put, TemplatePath, token, new { version, content });

    private static async Task<HttpResponseMessage> DownloadAsync(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, TemplatePath + "/source");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd("text/plain");
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task<HttpResponseMessage> SendPdfRequestAsync(HttpClient client, string token, string path, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd("application/pdf");
        request.Headers.Accept.ParseAdd("application/vnd.siren+json");
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task<Guid> CreateCharacterAsync(HttpClient client, string token)
    {
        using var player = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, PlayersPath, token,
            new { displayName = "Ann", type = "player" });
        var playerId = (await JsonAsync(player)).GetProperty("properties").GetProperty("playerId").GetGuid();
        using var character = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post,
            $"{PlayersPath}/{playerId:D}/characters", token, new { name = "Kira", experiencePoints = 0 });
        return (await JsonAsync(character)).GetProperty("properties").GetProperty("characterId").GetGuid();
    }

    private static async Task<string[]> PdfWordsAsync(HttpResponseMessage response)
    {
        using var pdf = PdfDocument.Open(await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        return pdf.GetPages().SelectMany(page => page.GetWords()).Select(word => word.Text).ToArray();
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement.Clone();

    private static string?[] ActionNames(JsonElement entity) =>
        entity.GetProperty("actions").EnumerateArray().Select(action => action.GetProperty("name").GetString()).ToArray();

    private static string?[] LinkHrefs(JsonElement entity) =>
        entity.GetProperty("links").EnumerateArray().Select(link => link.GetProperty("href").GetString()).ToArray();

    /// <param name="detailMatches">Whether Typst's error, in the <c>template</c> detail, says what is expected.</param>
    private static async Task<ErrorSnapshot> ErrorAsync(HttpResponseMessage response, Func<string, bool> detailMatches)
    {
        var properties = (await JsonAsync(response)).GetProperty("properties");
        var detail = properties.TryGetProperty("errors", out var errors) && errors.TryGetProperty("template", out var template)
            ? template[0].GetString() ?? string.Empty
            : null;
        return new ErrorSnapshot(response.StatusCode, properties.GetProperty("code").GetString(), detail is not null && detailMatches(detail));
    }

    private sealed record ErrorSnapshot(HttpStatusCode Status, string? Code, bool TemplateErrorMatches);
}
