using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.Skills;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Players;

[Collection("Backend database integration")]
public sealed class CharacterSkillsApiTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private const string PlayersPath = "/local/api/v1/players";
    private const string CharactersPath = "/local/api/v1/characters";

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CharacterLinksToItsTalentsAndSkills()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);

        using var response = await GetAsync(client, token, $"{CharactersPath}/{characterId:D}");

        Assert.Equal($"{CharactersPath}/{characterId:D}/skills", (await ReadAsync(response)).LinkHref(SirenRelations.CharacterSkills));
    }

    [Fact]
    public async Task TalentsAndSkillsLinkToTheCharacterAndBothCatalogs()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);

        using var response = await GetAsync(client, token, SkillsPath(characterId));

        var document = await ReadAsync(response);
        Assert.Equal(
            [$"{CharactersPath}/{characterId:D}", "/local/api/v1/talents", "/local/api/v1/skills"],
            [document.LinkHref(SirenRelations.Character), document.LinkHref(SirenRelations.Talents), document.LinkHref(SirenRelations.Skills)]);
    }

    [Fact]
    public async Task ReaderSeesNoSaveAction()
    {
        using var client = factory.CreateClient();
        var characterId = await CreateCharacterWithReaderAccountAsync(client);
        var token = await AdministratorAuthenticationFactory.SignInReaderAsync(client);

        using var response = await GetAsync(client, token, SkillsPath(characterId));

        Assert.Empty((await ReadAsync(response)).ActionNames());
    }

    [Fact]
    public async Task AdministratorSeesTheSaveAction()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);

        using var response = await GetAsync(client, token, SkillsPath(characterId));

        Assert.Equal(["save-character-skills"], (await ReadAsync(response)).ActionNames());
    }

    [Fact]
    public async Task ReaderCannotSaveTalentsAndSkills()
    {
        using var client = factory.CreateClient();
        var characterId = await CreateCharacterWithReaderAccountAsync(client);
        var token = await AdministratorAuthenticationFactory.SignInReaderAsync(client);

        using var response = await SendAsync(client, token, SkillsPath(characterId),
            new { version = 1, talentIds = Array.Empty<Guid>(), skills = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdministratorSavesTalentsAndSkillsAndSeesTheFreePoints()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token, experiencePoints: 30);
        var (talentId, skillId) = await CreateTalentWithSkillAsync("Pilot", "Flying", 10);

        using var response = await SendAsync(client, token, SkillsPath(characterId), new
        {
            version = 1,
            talentIds = new[] { talentId },
            skills = new[] { new { skillId, talentId, count = 1, specifics = "Shuttles" } }
        });

        Assert.Equal(20, (await ReadAsync(response)).Property("freePoints").GetInt64());
    }

    [Fact]
    public async Task MissingListsAreAValidationError()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);

        using var response = await SendAsync(client, token, SkillsPath(characterId), new { version = 1 });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.validation"), await ErrorAsync(response));
    }

    [Fact]
    public async Task NullSkillEntryIsAValidationError()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);

        using var response = await SendAsync(client, token, SkillsPath(characterId),
            new { version = 1, talentIds = Array.Empty<Guid>(), skills = new object?[] { null } });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.validation"), await ErrorAsync(response));
    }

    [Fact]
    public async Task InvalidPurchaseCountReturnsItsStableCode()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);
        var (talentId, skillId) = await CreateTalentWithSkillAsync("Pilot", "Flying", 10);

        using var response = await SendAsync(client, token, SkillsPath(characterId), new
        {
            version = 1,
            talentIds = new[] { talentId },
            skills = new[] { new { skillId, talentId, count = 0, specifics = (string?)null } }
        });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.invalidPurchaseCount"), await ErrorAsync(response));
    }

    [Fact]
    public async Task BrokenSkillRuleListsTheSkillNames()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token, experiencePoints: 30);
        var (talentId, flyingId) = await CreateTalentWithSkillAsync("Pilot", "Flying", 10);
        var aces = await factory.Services.GetRequiredService<ISkillService>().CreateSkillAsync(
            new SkillDefinition("Aces", null, 10, false, false, [talentId], [], [], [], [[flyingId]]),
            TestContext.Current.CancellationToken);

        using var response = await SendAsync(client, token, SkillsPath(characterId), new
        {
            version = 1,
            talentIds = new[] { talentId },
            skills = new[] { new { skillId = aces.Value!.SkillId, talentId, count = 1, specifics = (string?)null } }
        });

        var error = await ReadAsync(response);
        Assert.Equal(
            new { Status = HttpStatusCode.Conflict, Code = (string?)"errors.prerequisitesNotMet", Skills = "Aces" },
            new
            {
                Status = response.StatusCode,
                Code = error.Property("code").GetString(),
                Skills = string.Join(',', error.Property("errors").GetProperty("skills").EnumerateArray().Select(name => name.GetString()))
            });
    }

    [Fact]
    public async Task OlderVersionIsAStaleEdit()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);
        var (talentId, _) = await CreateTalentWithSkillAsync("Pilot", "Flying", 10);
        using var first = await SendAsync(client, token, SkillsPath(characterId),
            new { version = 1, talentIds = new[] { talentId }, skills = Array.Empty<object>() });

        using var response = await SendAsync(client, token, SkillsPath(characterId),
            new { version = 1, talentIds = Array.Empty<Guid>(), skills = Array.Empty<object>() });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.Conflict, "errors.staleVersion"), await ErrorAsync(response));
    }

    [Fact]
    public async Task UnknownCharacterHasNoTalentsAndSkills()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await GetAsync(client, token, SkillsPath(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static string SkillsPath(Guid characterId) => $"{CharactersPath}/{characterId:D}/skills";

    private async Task<(Guid TalentId, Guid SkillId)> CreateTalentWithSkillAsync(string talent, string skill, int cost)
    {
        var talentId = (await factory.Services.GetRequiredService<ITalentService>()
            .CreateTalentAsync(new TalentDefinition(talent, null), TestContext.Current.CancellationToken)).Value!.TalentId;
        var skillId = (await factory.Services.GetRequiredService<ISkillService>()
            .CreateSkillAsync(new SkillDefinition(skill, null, cost, false, false, [talentId], [], [], [], []),
                TestContext.Current.CancellationToken)).Value!.SkillId;
        return (talentId, skillId);
    }

    private async Task<string> AdministratorTokenAsync(HttpClient client)
    {
        await factory.BootstrapAdministratorAsync();
        return await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");
    }

    /// <summary>Creates a reader account, which bootstraps the administrator, and a character made by that administrator.</summary>
    private async Task<Guid> CreateCharacterWithReaderAccountAsync(HttpClient client)
    {
        await factory.CreateReaderAccountAsync(client);
        return await CreateCharacterAsync(client, await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1"));
    }

    private static async Task<Guid> CreateCharacterAsync(HttpClient client, string token, int experiencePoints = 0)
    {
        using var player = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, PlayersPath, token,
            new { displayName = "Ann", type = "player" });
        var playerId = (await ReadAsync(player)).Property("playerId").GetGuid();
        using var character = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post,
            $"{PlayersPath}/{playerId:D}/characters", token, new { name = "Kira", experiencePoints });
        return (await ReadAsync(character)).Property("characterId").GetGuid();
    }

    private static Task<HttpResponseMessage> GetAsync(HttpClient client, string token, string path) =>
        AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, path, token);

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string token, string path, object body) =>
        AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Put, path, token, body);

    private static async Task<SirenDocument> ReadAsync(HttpResponseMessage response) =>
        new(JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement.Clone());

    private static async Task<ErrorSnapshot> ErrorAsync(HttpResponseMessage response) =>
        new(response.StatusCode, (await ReadAsync(response)).Property("code").GetString());

    private sealed record ErrorSnapshot(HttpStatusCode Status, string? Code);

    private sealed record SirenDocument(JsonElement Root)
    {
        public JsonElement Property(string name) => Root.GetProperty("properties").GetProperty(name);

        public string LinkHref(string rel) =>
            Root.GetProperty("links").EnumerateArray()
                .Where(link => link.GetProperty("rel").EnumerateArray().Any(value => value.GetString() == rel))
                .Select(link => link.GetProperty("href").GetString())
                .FirstOrDefault() ?? throw new InvalidOperationException($"No {rel} link.");

        public string?[] ActionNames() =>
            Root.TryGetProperty("actions", out var actions)
                ? actions.EnumerateArray().Select(action => action.GetProperty("name").GetString()).ToArray()
                : [];
    }
}
