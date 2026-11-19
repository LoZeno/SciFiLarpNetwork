using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Application.Skills;
using SciFiLarpNetwork.Backend.Domain.CharacterSkills;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;
using UglyToad.PdfPig;

namespace SciFiLarpNetwork.Backend.Tests.Integration.CharacterCards;

[Collection("Backend database integration")]
public sealed class CharacterCardsApiTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private const string PlayersPath = "/local/api/v1/players";
    private const string CardsPath = "/local/api/v1/character-cards";

    private static readonly string[] FrontAndBackWords =
        ["Player", "Ann", "Character", "CAPTAIN", "Kira", "XP:", "30", "Free", "points:", "20", "SKILLS", "Combat:", "Blaster"];

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task ReaderCanPrintCardsAsPdf()
    {
        using var client = factory.CreateClient();
        await factory.CreateReaderAccountAsync(client);
        var characterId = await CreateCharacterAsync(client,
            await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1"));
        var token = await AdministratorAuthenticationFactory.SignInReaderAsync(client);

        using var response = await PrintAsync(client, token, [characterId]);

        Assert.Equal(
            new { Status = HttpStatusCode.OK, MediaType = (string?)"application/pdf" },
            new { Status = response.StatusCode, response.Content.Headers.ContentType?.MediaType });
    }

    [Fact]
    public async Task CardPrintsTheFrontAndTheBack()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token, "Kira", "Captain", experiencePoints: 30);
        await BuySkillAsync(characterId, "Combat", "Blaster", cost: 10);

        using var response = await PrintAsync(client, token, [characterId]);

        Assert.Empty(FrontAndBackWords.Except(await PdfWordsAsync(response)));
    }

    [Fact]
    public async Task ArchivedCharacterCardSaysArchived()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);
        await ArchiveAsync(client, token, characterId);

        using var response = await PrintAsync(client, token, [characterId]);

        Assert.Contains("ARCHIVED", await PdfWordsAsync(response));
    }

    [Fact]
    public async Task LabelsUseTheRequestLanguage()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);

        using var response = await PrintAsync(client, token, [characterId], language: "it");

        Assert.Contains("Giocatore", await PdfWordsAsync(response));
    }

    [Fact]
    public async Task FourCardsFillAPageAndAFifthStartsTheNext()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);

        using var response = await PrintAsync(client, token, Enumerable.Repeat(characterId, 5).ToArray());

        Assert.Equal(2, PdfDocument.Open(await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken)).NumberOfPages);
    }

    [Fact]
    public async Task EmptySelectionReturnsItsStableCode()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await PrintAsync(client, token, []);

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.noCharactersSelected"), await ErrorAsync(response));
    }

    [Fact]
    public async Task TooManyCardsReturnsItsStableCode()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);

        using var response = await PrintAsync(client, token, Enumerable.Repeat(characterId, 201).ToArray());

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.tooManyCharacters"), await ErrorAsync(response));
    }

    [Fact]
    public async Task UnknownCharacterReturnsItsStableCode()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);

        using var response = await PrintAsync(client, token, [characterId, Guid.NewGuid()]);

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.unknownCharacters"), await ErrorAsync(response));
    }

    [Fact]
    public async Task UnknownCharactersAreListedInTheError()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);
        var unknownId = Guid.NewGuid();

        using var response = await PrintAsync(client, token, [characterId, unknownId]);

        Assert.Equal([unknownId.ToString("D")], await ErrorDetailAsync(response, "characterIds"));
    }

    [Fact]
    public async Task MissingCharacterListIsAValidationError()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await SendAsync(client, token, new { }, language: null);

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.validation"), await ErrorAsync(response));
    }

    [Fact]
    public async Task TypstFailureReturnsItsStableCode()
    {
        using var brokenFactory = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("CardPrinting:TypstPath", Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}", "typst")));
        using var client = brokenFactory.CreateClient();
        await factory.BootstrapAdministratorAsync();
        var token = await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");
        var characterId = await CreateCharacterAsync(client, token);

        using var response = await PrintAsync(client, token, [characterId]);

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.InternalServerError, "errors.cardRenderingFailed"), await ErrorAsync(response));
    }

    private async Task<string> AdministratorTokenAsync(HttpClient client)
    {
        await factory.BootstrapAdministratorAsync();
        return await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");
    }

    private static async Task<Guid> CreateCharacterAsync(
        HttpClient client,
        string token,
        string name = "Kira",
        string? title = null,
        int experiencePoints = 0)
    {
        using var player = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, PlayersPath, token,
            new { displayName = "Ann", type = "player" });
        var playerId = (await JsonAsync(player)).GetProperty("properties").GetProperty("playerId").GetGuid();
        using var character = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post,
            $"{PlayersPath}/{playerId:D}/characters", token, new { name, title, experiencePoints });
        return (await JsonAsync(character)).GetProperty("properties").GetProperty("characterId").GetGuid();
    }

    private static async Task ArchiveAsync(HttpClient client, string token, Guid characterId)
    {
        var path = $"/local/api/v1/characters/{characterId:D}";
        using var current = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, path, token);
        var version = (await JsonAsync(current)).GetProperty("properties").GetProperty("version").GetInt64();
        using var retired = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Put, path, token,
            new { version, name = "Kira", experiencePoints = 0, status = "retired" });
        retired.EnsureSuccessStatusCode();
        using var archived = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Post, $"{path}/archive", token);
        archived.EnsureSuccessStatusCode();
    }

    private async Task BuySkillAsync(Guid characterId, string talent, string skill, int cost)
    {
        var talentId = (await factory.Services.GetRequiredService<ITalentService>()
            .CreateTalentAsync(new TalentDefinition(talent, null), TestContext.Current.CancellationToken)).Value!.TalentId;
        var skillId = (await factory.Services.GetRequiredService<ISkillService>()
            .CreateSkillAsync(new SkillDefinition(skill, null, cost, false, false, [talentId], [], [], [], []),
                TestContext.Current.CancellationToken)).Value!.SkillId;
        var result = await factory.Services.GetRequiredService<ICharacterSkillService>()
            .SaveCharacterSkillsAsync(characterId, 1,
                new CharacterSkillsDefinition(
                    [talentId], [new SkillPurchase(skillId, talentId, 1, null)]),
                TestContext.Current.CancellationToken);
        Assert.Equal(RosterOutcome.Succeeded, result.Outcome);
    }

    private static Task<HttpResponseMessage> PrintAsync(HttpClient client, string token, Guid[] characterIds, string? language = null) =>
        SendAsync(client, token, new { characterIds }, language);

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, string token, object body, string? language)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, CardsPath)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd("application/pdf");
        request.Headers.Accept.ParseAdd("application/vnd.siren+json");
        if (language is not null)
        {
            request.Headers.AcceptLanguage.ParseAdd(language);
        }

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task<string[]> PdfWordsAsync(HttpResponseMessage response)
    {
        using var pdf = PdfDocument.Open(await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        return pdf.GetPages().SelectMany(page => page.GetWords()).Select(word => word.Text).ToArray();
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement.Clone();

    private static async Task<ErrorSnapshot> ErrorAsync(HttpResponseMessage response) =>
        new(response.StatusCode, (await JsonAsync(response)).GetProperty("properties").GetProperty("code").GetString());

    private static async Task<string?[]> ErrorDetailAsync(HttpResponseMessage response, string key) =>
        (await JsonAsync(response)).GetProperty("properties").GetProperty("errors").GetProperty(key).EnumerateArray()
        .Select(value => value.GetString())
        .ToArray();

    private sealed record ErrorSnapshot(HttpStatusCode Status, string? Code);
}
