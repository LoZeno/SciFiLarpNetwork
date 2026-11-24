using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;
using SciFiLarpNetwork.Common.Siren;
using UglyToad.PdfPig;

namespace SciFiLarpNetwork.Backend.Tests.Integration.LarpEvents;

[Collection("Backend database integration")]
public sealed class LarpEventsApiTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private const string EventsPath = "/local/api/v1/events";
    private const string PlayersPath = "/local/api/v1/players";
    private static readonly string[] ValidityWords = ["Valid:", "2026-05-01", "2026-05-02"];

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task VersionRootAdvertisesEventsToReaders()
    {
        using var client = factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await GetAsync(client, token, "/local/api/v1");

        Assert.Equal(EventsPath, (await ReadAsync(response)).LinkHref(SirenRelations.Events));
    }

    [Fact]
    public async Task AnonymousRequestCannotListEvents()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(EventsPath, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReaderCannotCreateEvents()
    {
        using var client = factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await SendAsync(client, token, HttpMethod.Post, EventsPath, new { name = "Fort" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReaderSeesNoCreateAction()
    {
        using var client = factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await GetAsync(client, token, EventsPath);

        Assert.Empty((await ReadAsync(response)).ActionNames());
    }

    [Fact]
    public async Task AdministratorCreatesAnEvent()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await SendAsync(client, token, HttpMethod.Post, EventsPath, new { name = " Fort ", description = "Tents" });

        Assert.Equal(
            new { Status = HttpStatusCode.Created, Name = (string?)"Fort" },
            new { Status = response.StatusCode, Name = (await ReadAsync(response)).Property("name").GetString() });
    }

    [Fact]
    public async Task InvalidEventReturnsItsStableCode()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await SendAsync(client, token, HttpMethod.Post, EventsPath, new { name = new string('n', 65) });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.invalidName"), await ErrorAsync(response));
    }

    [Fact]
    public async Task DescriptionWithANullCharacterReturnsItsStableCode()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await SendAsync(client, token, HttpMethod.Post, EventsPath, new { name = "Fort", description = "a\0b" });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.invalidDescription"), await ErrorAsync(response));
    }

    [Fact]
    public async Task AdministratorEditsAnEvent()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var eventPath = await CreateEventAsync(client, token);

        using var response = await SendAsync(client, token, HttpMethod.Put, eventPath, new { version = 1, name = "Keep", description = "Walls" });

        Assert.Equal("Keep", (await ReadAsync(response)).Property("name").GetString());
    }

    [Fact]
    public async Task StaleEditReturnsItsStableCode()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var eventPath = await CreateEventAsync(client, token);
        (await SendAsync(client, token, HttpMethod.Put, eventPath, new { version = 1, name = "Keep" })).Dispose();

        using var response = await SendAsync(client, token, HttpMethod.Put, eventPath, new { version = 1, name = "Tower" });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.Conflict, "errors.staleVersion"), await ErrorAsync(response));
    }

    [Fact]
    public async Task OpenEventOffersEveryChangeToAdministrators()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var eventPath = await CreateEventAsync(client, token);

        using var response = await GetAsync(client, token, eventPath);

        Assert.Equal(
            "list-participants,print-event-cards,update-event,add-day,update-day,remove-day,register-participant,unregister-participant,change-experience,close-event",
            string.Join(',', (await ReadAsync(response)).ActionNames()));
    }

    [Fact]
    public async Task ClosedEventOffersOnlyListPrintReopenAndArchive()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var eventPath = await CreateEventAsync(client, token);
        await CloseAsync(client, token, eventPath);

        using var response = await GetAsync(client, token, eventPath);

        Assert.Equal("list-participants,print-event-cards,reopen-event,archive-event", string.Join(',', (await ReadAsync(response)).ActionNames()));
    }

    [Fact]
    public async Task ReaderSeesOnlyTheListAndPrintActionsOnAnEvent()
    {
        using var client = factory.CreateClient();
        await factory.CreateReaderAccountAsync(client);
        var eventPath = await CreateEventAsync(client, await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1"));
        var token = await AdministratorAuthenticationFactory.SignInReaderAsync(client);

        using var response = await GetAsync(client, token, eventPath);

        Assert.Equal("list-participants,print-event-cards", string.Join(',', (await ReadAsync(response)).ActionNames()));
    }

    [Fact]
    public async Task ClosedEventRejectsNewDaysWithItsStableCode()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var eventPath = await CreateEventAsync(client, token);
        await CloseAsync(client, token, eventPath);

        using var response = await PostAsync(client, token, $"{eventPath}/days", new { date = "2026-05-01" });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.Conflict, "errors.eventClosed"), await ErrorAsync(response));
    }

    [Fact]
    public async Task ClosingWithAwardsGivesTheCharacterExperience()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        using var created = await SendAsync(client, token, HttpMethod.Post, EventsPath, new { name = "Fort", baseExperience = 4 });
        var eventPath = (await ReadAsync(created)).LinkHref("self");
        using var withDay = await PostAsync(client, token, $"{eventPath}/days", new { date = "2026-05-01", experience = 6 });
        var dayId = (await ReadAsync(withDay)).Property("days")[0].GetProperty("dayId").GetGuid();
        var characterId = await CreateCharacterAsync(client, token);
        using var registered = await PostAsync(client, token, $"{eventPath}/participants", new { characterId, dayIds = new[] { dayId } });
        var version = (await ReadAsync(registered)).Property("version").GetInt64();

        (await PostAsync(client, token, $"{eventPath}/close",
            new { version, awards = new[] { new { characterId, baseExperience = true, dayIds = new[] { dayId } } } })).Dispose();

        using var character = await GetAsync(client, token, $"/local/api/v1/characters/{characterId:D}");
        Assert.Equal(10, (await ReadAsync(character)).Property("experiencePoints").GetInt32());
    }

    [Fact]
    public async Task ParticipantsListShowsRegisteredCharacters()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var (eventPath, _) = await RegisteredCharacterAsync(client, token);

        using var response = await GetAsync(client, token, $"{eventPath}/participants");

        Assert.Equal(["Kira"], (await ReadAsync(response)).EntityValues("characterName"));
    }

    [Fact]
    public async Task ExperienceChangeNamesTheAdministrator()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var (eventPath, characterId) = await RegisteredCharacterAsync(client, token);
        (await PostAsync(client, token, $"{eventPath}/participants/experience", new { characterId, amount = 4 })).Dispose();

        using var response = await GetAsync(client, token, $"{eventPath}/participants");

        Assert.Equal("local-admin", (await ReadAsync(response)).Root.GetProperty("entities")[0].GetProperty("properties")
            .GetProperty("experienceChanges")[0].GetProperty("changedBy").GetString());
    }

    [Fact]
    public async Task EventCardsPrintTheValidityDates()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var (eventPath, _) = await RegisteredCharacterAsync(client, token);

        using var response = await PrintAsync(client, token, $"{eventPath}/cards", new { dayId = (Guid?)null });

        Assert.Empty(ValidityWords.Except(await PdfWordsAsync(response)));
    }

    [Fact]
    public async Task EventWithoutParticipantsHasNothingToPrint()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var eventPath = await CreateEventAsync(client, token);

        using var response = await PrintAsync(client, token, $"{eventPath}/cards", new { dayId = (Guid?)null });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.noParticipants"), await ErrorAsync(response));
    }

    private static async Task<(string EventPath, Guid CharacterId)> RegisteredCharacterAsync(HttpClient client, string token)
    {
        var eventPath = await CreateEventAsync(client, token);
        (await PostAsync(client, token, $"{eventPath}/days", new { date = "2026-05-01" })).Dispose();
        using var withDays = await PostAsync(client, token, $"{eventPath}/days", new { date = "2026-05-02" });
        var dayIds = (await ReadAsync(withDays)).Property("days").EnumerateArray()
            .Select(day => day.GetProperty("dayId").GetGuid()).ToArray();
        var characterId = await CreateCharacterAsync(client, token);
        using var registered = await PostAsync(client, token, $"{eventPath}/participants", new { characterId, dayIds });
        registered.EnsureSuccessStatusCode();
        return (eventPath, characterId);
    }

    private static async Task CloseAsync(HttpClient client, string token, string eventPath)
    {
        using var current = await GetAsync(client, token, eventPath);
        var version = (await ReadAsync(current)).Property("version").GetInt64();
        using var closed = await PostAsync(client, token, $"{eventPath}/close", new { version, awards = Array.Empty<object>() });
        closed.EnsureSuccessStatusCode();
    }

    private static async Task<string> CreateEventAsync(HttpClient client, string token)
    {
        using var response = await SendAsync(client, token, HttpMethod.Post, EventsPath, new { name = "Fort" });
        response.EnsureSuccessStatusCode();
        return (await ReadAsync(response)).LinkHref("self");
    }

    private static async Task<Guid> CreateCharacterAsync(HttpClient client, string token)
    {
        using var player = await SendAsync(client, token, HttpMethod.Post, PlayersPath, new { displayName = "Ann", type = "player" });
        var playerId = (await ReadAsync(player)).Property("playerId").GetGuid();
        using var character = await SendAsync(client, token, HttpMethod.Post, $"{PlayersPath}/{playerId:D}/characters",
            new { name = "Kira", experiencePoints = 0 });
        return (await ReadAsync(character)).Property("characterId").GetGuid();
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

    private static Task<HttpResponseMessage> GetAsync(HttpClient client, string token, string path) =>
        AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, path, token);

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string token, string path, object body) =>
        SendAsync(client, token, HttpMethod.Post, path, body);

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string token, HttpMethod method, string path, object body) =>
        AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, method, path, token, body);

    private static async Task<HttpResponseMessage> PrintAsync(HttpClient client, string token, string path, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd("application/pdf");
        request.Headers.Accept.ParseAdd("application/vnd.siren+json");
        return await client.SendAsync(request, Token);
    }

    private static async Task<string[]> PdfWordsAsync(HttpResponseMessage response)
    {
        using var pdf = PdfDocument.Open(await response.Content.ReadAsByteArrayAsync(Token));
        return pdf.GetPages().SelectMany(page => page.GetWords()).Select(word => word.Text).ToArray();
    }

    private static async Task<SirenDocument> ReadAsync(HttpResponseMessage response) =>
        new(JsonDocument.Parse(await response.Content.ReadAsStringAsync(Token)).RootElement.Clone());

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

        public string?[] EntityValues(string property) =>
            Root.GetProperty("entities").EnumerateArray()
                .Select(entity => entity.GetProperty("properties").GetProperty(property).GetString())
                .ToArray();
    }
}
