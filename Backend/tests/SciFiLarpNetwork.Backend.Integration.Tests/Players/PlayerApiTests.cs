using System.Net;
using System.Text.Json;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Players;

[Collection("Backend database integration")]
public sealed class PlayerApiTests : IAsyncLifetime
{
    private const string PlayersPath = "/local/api/v1/players";
    private const string CharactersPath = "/local/api/v1/characters";
    private const string StatsPath = "/local/api/v1/character-stats";
    private readonly AdministratorAuthenticationFactory _factory = new();

    public async ValueTask InitializeAsync()
    {
        await _factory.InitializeAsync();
        await _factory.Services.GetRequiredService<IDocumentStore>().Advanced.ResetAllData();
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task VersionRootAdvertisesPlayersToReaders()
    {
        using var client = _factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await GetAsync(client, token, "/local/api/v1");

        Assert.Equal(PlayersPath, (await ReadAsync(response)).LinkHref(SirenRelations.Players));
    }

    [Fact]
    public async Task AnonymousRequestCannotListPlayers()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(PlayersPath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReaderSeesOnlyTheSearchActions()
    {
        using var client = _factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await GetAsync(client, token, PlayersPath);

        Assert.Equal("search-players,search-characters,find-character-by-qr", string.Join(',', (await ReadAsync(response)).ActionNames()));
    }

    [Fact]
    public async Task AdministratorAlsoSeesTheCreatePlayerAction()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await GetAsync(client, token, PlayersPath);

        Assert.Contains("create-player", (await ReadAsync(response)).ActionNames());
    }

    [Fact]
    public async Task ReaderCannotCreatePlayers()
    {
        using var client = _factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await SendAsync(client, token, HttpMethod.Post, PlayersPath, new { displayName = "Ann", type = "player" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdministratorCreatesAPlayer()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await SendAsync(client, token, HttpMethod.Post, PlayersPath,
            new { displayName = " Ann ", type = "gameMaster" });
        var player = await ReadAsync(response);

        Assert.Equal(
            new { Status = HttpStatusCode.Created, Number = 1, Name = (string?)"Ann", Type = (string?)"gameMaster", PlayerStatus = (string?)"active" },
            new
            {
                Status = response.StatusCode,
                Number = player.Property("number").GetInt32(),
                Name = player.Property("displayName").GetString(),
                Type = player.Property("type").GetString(),
                PlayerStatus = player.Property("status").GetString()
            });
    }

    [Fact]
    public async Task UnknownPlayerTypeIsAValidationError()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await SendAsync(client, token, HttpMethod.Post, PlayersPath, new { displayName = "Ann", type = "boss" });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.validation"), await ErrorAsync(response));
    }

    [Fact]
    public async Task InvalidEmailReturnsItsStableCode()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await SendAsync(client, token, HttpMethod.Post, PlayersPath,
            new { displayName = "Ann", email = "not an email", type = "player" });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.invalidEmail"), await ErrorAsync(response));
    }

    [Fact]
    public async Task StalePlayerEditReturnsTheStaleVersionCode()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var playerId = await CreatePlayerAsync(client, token, "Ann");
        using var first = await SendAsync(client, token, HttpMethod.Put, $"{PlayersPath}/{playerId:D}",
            new { version = 1, displayName = "Ann B", type = "player" });

        using var response = await SendAsync(client, token, HttpMethod.Put, $"{PlayersPath}/{playerId:D}",
            new { version = 1, displayName = "Ann C", type = "player" });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.Conflict, "errors.staleVersion"), await ErrorAsync(response));
    }

    [Fact]
    public async Task ActivePlayerAdvertisesEditCharacterCreationAndDeactivation()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var playerId = await CreatePlayerAsync(client, token, "Ann");

        using var response = await GetAsync(client, token, $"{PlayersPath}/{playerId:D}");

        Assert.Equal("update-player,create-character,deactivate-player", string.Join(',', (await ReadAsync(response)).ActionNames()));
    }

    [Fact]
    public async Task InactivePlayerAdvertisesActivationAndArchiving()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var playerId = await CreatePlayerAsync(client, token, "Ann");

        using var response = await SendAsync(client, token, HttpMethod.Post, $"{PlayersPath}/{playerId:D}/deactivate", new { });

        Assert.Equal("update-player,activate-player,archive-player", string.Join(',', (await ReadAsync(response)).ActionNames()));
    }

    [Fact]
    public async Task ArchivingAnActivePlayerReturnsItsStableCode()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var playerId = await CreatePlayerAsync(client, token, "Ann");

        using var response = await SendAsync(client, token, HttpMethod.Post, $"{PlayersPath}/{playerId:D}/archive", new { });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.Conflict, "errors.playerNotInactive"), await ErrorAsync(response));
    }

    [Fact]
    public async Task SearchQueryFiltersThePlayers()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        await CreatePlayerAsync(client, token, "Ann");
        await CreatePlayerAsync(client, token, "Bob");

        using var response = await GetAsync(client, token, $"{PlayersPath}?search=bo");

        Assert.Equal("Bob", string.Join(',', (await ReadAsync(response)).EntityValues("displayName")));
    }

    [Fact]
    public async Task SinglePageHasNoPreviousOrNextLink()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        await CreatePlayerAsync(client, token, "Ann");

        using var response = await GetAsync(client, token, PlayersPath);
        var page = await ReadAsync(response);

        Assert.Equal(
            new { Previous = (string?)null, Next = (string?)null, Last = (string?)PlayersPath },
            new
            {
                Previous = page.LinkHrefOrNull(SirenRelations.Previous),
                Next = page.LinkHrefOrNull(SirenRelations.Next),
                Last = page.LinkHrefOrNull(SirenRelations.Last)
            });
    }

    [Fact]
    public async Task LaterPageLinksBackKeepingTheSearchAndFilter()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        await CreatePlayerAsync(client, token, "Ann");

        using var response = await GetAsync(client, token, $"{PlayersPath}?search=a+b&includeArchived=true&page=3");

        Assert.Equal($"{PlayersPath}?search=a%20b&includeArchived=true",
            (await ReadAsync(response)).LinkHrefOrNull(SirenRelations.Previous));
    }

    [Fact]
    public async Task PageZeroIsAValidationError()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await GetAsync(client, token, $"{PlayersPath}?page=0");

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.validation"), await ErrorAsync(response));
    }

    [Fact]
    public async Task AdministratorCreatesACharacterForAPlayer()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var playerId = await CreatePlayerAsync(client, token, "Ann");

        using var response = await SendAsync(client, token, HttpMethod.Post, $"{PlayersPath}/{playerId:D}/characters",
            new { name = "Kira", experiencePoints = 120, newStats = new[] { new { name = "Rank", type = "number", number = 2 } } });
        var character = await ReadAsync(response);

        Assert.Equal(
            new { Status = HttpStatusCode.Created, Name = (string?)"Kira", Points = 120, CharacterStatus = (string?)"active", Stats = 1 },
            new
            {
                Status = response.StatusCode,
                Name = character.Property("name").GetString(),
                Points = character.Property("experiencePoints").GetInt32(),
                CharacterStatus = character.Property("status").GetString(),
                Stats = character.Property("stats").GetArrayLength()
            });
    }

    [Fact]
    public async Task CharacterCreationCannotSetAStatus()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var playerId = await CreatePlayerAsync(client, token, "Ann");

        using var response = await SendAsync(client, token, HttpMethod.Post, $"{PlayersPath}/{playerId:D}/characters",
            new { name = "Kira", experiencePoints = 0, status = "dead" });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.validation"), await ErrorAsync(response));
    }

    [Fact]
    public async Task ActiveCharacterAdvertisesEditAndNewQrButNotArchiving()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token, await CreatePlayerAsync(client, token, "Ann"), "Kira");

        using var response = await GetAsync(client, token, $"{CharactersPath}/{characterId:D}");

        Assert.Equal("update-character,regenerate-qr-identifier", string.Join(',', (await ReadAsync(response)).ActionNames()));
    }

    [Fact]
    public async Task DeadCharacterAdvertisesArchiving()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token, await CreatePlayerAsync(client, token, "Ann"), "Kira");

        using var response = await SendAsync(client, token, HttpMethod.Put, $"{CharactersPath}/{characterId:D}",
            new { version = 1, name = "Kira", experiencePoints = 0, status = "dead", deathDate = "2026-05-01" });

        Assert.Equal("update-character,regenerate-qr-identifier,archive-character", string.Join(',', (await ReadAsync(response)).ActionNames()));
    }

    [Fact]
    public async Task DisabledNetworkReturnsItsStableCode()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var playerId = await CreatePlayerAsync(client, token, "Ann");
        using var network = await SendAsync(client, token, HttpMethod.Post, "/local/api/v1/networks", new { code = "rim", name = "Rim", theme = "blue" });
        var networkId = (await ReadAsync(network)).Property("networkId").GetGuid();
        using var disabled = await SendAsync(client, token, HttpMethod.Post, $"/local/api/v1/networks/{networkId:D}/disable", new { });

        using var response = await SendAsync(client, token, HttpMethod.Post, $"{PlayersPath}/{playerId:D}/characters",
            new { name = "Kira", experiencePoints = 0, networkId });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.disabledNetworkNotSelectable"), await ErrorAsync(response));
    }

    [Fact]
    public async Task CharacterSearchListsMatchesAcrossPlayers()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        await CreateCharacterAsync(client, token, await CreatePlayerAsync(client, token, "Ann"), "Kira");
        await CreateCharacterAsync(client, token, await CreatePlayerAsync(client, token, "Bob"), "Kiran");

        using var response = await GetAsync(client, token, $"{CharactersPath}?search=kir");

        Assert.Equal("Kira,Kiran", string.Join(',', (await ReadAsync(response)).EntityValues("name")));
    }

    [Fact]
    public async Task QrLookupFindsTheCharacter()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token, await CreatePlayerAsync(client, token, "Ann"), "Kira");
        using var character = await GetAsync(client, token, $"{CharactersPath}/{characterId:D}");
        var identifier = (await ReadAsync(character)).Property("qrIdentifier").GetString();

        using var response = await GetAsync(client, token, $"{CharactersPath}/by-qr?identifier={identifier}");

        Assert.Equal(characterId, (await ReadAsync(response)).Property("characterId").GetGuid());
    }

    [Fact]
    public async Task MalformedQrIdentifierReturnsItsStableCode()
    {
        using var client = _factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await GetAsync(client, token, $"{CharactersPath}/by-qr?identifier=nope");

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.invalidQrIdentifier"), await ErrorAsync(response));
    }

    [Fact]
    public async Task AdministratorSeesStatRenameAndArchiveActions()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var playerId = await CreatePlayerAsync(client, token, "Ann");
        using var created = await SendAsync(client, token, HttpMethod.Post, $"{PlayersPath}/{playerId:D}/characters",
            new { name = "Kira", experiencePoints = 0, newStats = new[] { new { name = "Rank", type = "text", text = "high" } } });
        using var stats = await GetAsync(client, token, StatsPath);
        var statHref = (await ReadAsync(stats)).FirstEntityHref();

        using var response = await GetAsync(client, token, statHref);

        Assert.Equal("rename-character-stat,archive-character-stat", string.Join(',', (await ReadAsync(response)).ActionNames()));
    }

    private async Task<string> AdministratorTokenAsync(HttpClient client)
    {
        await _factory.BootstrapAdministratorAsync();
        return await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");
    }

    private async Task<string> ReaderTokenAsync(HttpClient client)
    {
        await _factory.CreateReaderAccountAsync(client);
        return await AdministratorAuthenticationFactory.SignInReaderAsync(client);
    }

    private static async Task<Guid> CreatePlayerAsync(HttpClient client, string token, string name)
    {
        using var response = await SendAsync(client, token, HttpMethod.Post, PlayersPath, new { displayName = name, type = "player" });
        response.EnsureSuccessStatusCode();
        return (await ReadAsync(response)).Property("playerId").GetGuid();
    }

    private static async Task<Guid> CreateCharacterAsync(HttpClient client, string token, Guid playerId, string name)
    {
        using var response = await SendAsync(client, token, HttpMethod.Post, $"{PlayersPath}/{playerId:D}/characters",
            new { name, experiencePoints = 0 });
        response.EnsureSuccessStatusCode();
        return (await ReadAsync(response)).Property("characterId").GetGuid();
    }

    private static Task<HttpResponseMessage> GetAsync(HttpClient client, string token, string path) =>
        AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, path, token);

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string token, HttpMethod method, string path, object body) =>
        AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, method, path, token, body);

    private static async Task<SirenDocument> ReadAsync(HttpResponseMessage response) =>
        new(JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement.Clone());

    private static async Task<ErrorSnapshot> ErrorAsync(HttpResponseMessage response) =>
        new(response.StatusCode, (await ReadAsync(response)).Property("code").GetString());

    private sealed record ErrorSnapshot(HttpStatusCode Status, string? Code);

    private sealed record SirenDocument(JsonElement Root)
    {
        public JsonElement Property(string name) => Root.GetProperty("properties").GetProperty(name);

        public string LinkHref(string rel) => LinkHrefOrNull(rel) ?? throw new InvalidOperationException($"No {rel} link.");

        public string? LinkHrefOrNull(string rel) =>
            Root.TryGetProperty("links", out var links)
                ? links.EnumerateArray()
                    .Where(link => link.GetProperty("rel").EnumerateArray().Any(value => value.GetString() == rel))
                    .Select(link => link.GetProperty("href").GetString())
                    .FirstOrDefault()
                : null;

        public string FirstEntityHref() => Root.GetProperty("entities")[0].GetProperty("href").GetString()!;

        public string?[] ActionNames() =>
            Root.TryGetProperty("actions", out var actions)
                ? actions.EnumerateArray().Select(action => action.GetProperty("name").GetString()).ToArray()
                : [];

        public string?[] EntityValues(string property) =>
            Root.GetProperty("entities").EnumerateArray()
                .Select(entity => entity.GetProperty("properties").GetProperty(property).GetString())
                .ToArray();
    }
}
