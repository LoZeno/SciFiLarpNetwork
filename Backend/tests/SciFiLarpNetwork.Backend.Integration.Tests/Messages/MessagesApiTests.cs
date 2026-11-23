using System.Net;
using System.Text.Json;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Messages;

[Collection("Backend database integration")]
public sealed class MessagesApiTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private const string MessagesPath = "/local/api/v1/messages";
    private const string PlayersPath = "/local/api/v1/players";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task VersionRootAdvertisesMessagesToReaders()
    {
        using var client = factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await GetAsync(client, token, "/local/api/v1");

        Assert.Equal(MessagesPath, (await ReadAsync(response)).LinkHref(SirenRelations.Messages));
    }

    [Fact]
    public async Task AnonymousRequestCannotListMessages()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(MessagesPath, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReaderGetsOnlyTheFilterAction()
    {
        using var client = factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await GetAsync(client, token, MessagesPath);

        Assert.Equal(["filter-messages"], (await ReadAsync(response)).ActionNames());
    }

    [Fact]
    public async Task AdministratorGetsTheFilterAndSendActions()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await GetAsync(client, token, MessagesPath);

        Assert.Equal(["filter-messages", "send-message"], (await ReadAsync(response)).ActionNames());
    }

    [Fact]
    public async Task CollectionCarriesTheMessageLimits()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await GetAsync(client, token, MessagesPath);

        var document = await ReadAsync(response);
        Assert.Equal(
            new { Recipients = 200, Title = 128, Body = 10000 },
            new
            {
                Recipients = document.Property("maximumRecipients").GetInt32(),
                Title = document.Property("maximumTitleLength").GetInt32(),
                Body = document.Property("maximumBodyLength").GetInt32()
            });
    }

    [Fact]
    public async Task ReaderCannotSendMessages()
    {
        using var client = factory.CreateClient();
        var (adminToken, readerToken, _) = await BothTokensAsync(client);
        var characterId = await CreateCharacterAsync(client, adminToken);

        using var response = await PostAsync(client, readerToken, MessagesPath, Message(characterId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdministratorSendsAMessageAndIsNamedAsSender()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);

        using var response = await PostAsync(client, token, MessagesPath, Message(characterId));

        Assert.Equal(
            new { Status = HttpStatusCode.Created, SentBy = (string?)"local-admin" },
            new { Status = response.StatusCode, SentBy = (await ReadAsync(response)).Property("sentBy").GetString() });
    }

    [Fact]
    public async Task RepeatedSubmissionReturnsTheSameMessage()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);
        var message = Message(characterId);
        (await PostAsync(client, token, MessagesPath, message)).Dispose();

        using var repeated = await PostAsync(client, token, MessagesPath, message);

        using var list = await GetAsync(client, token, MessagesPath);
        Assert.Equal(
            new { Status = HttpStatusCode.Created, Total = 1L },
            new { Status = repeated.StatusCode, Total = (await ReadAsync(list)).Property("totalCount").GetInt64() });
    }

    [Fact]
    public async Task InvalidTitleReturnsItsStableCode()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);

        using var response = await PostAsync(client, token, MessagesPath, Message(characterId) with { title = " " });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.invalidMessageTitle"), await ErrorAsync(response));
    }

    [Fact]
    public async Task BodyWithANullCharacterReturnsItsStableCode()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);

        using var response = await PostAsync(client, token, MessagesPath, Message(characterId) with { body = "a\0b" });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.invalidMessageBody"), await ErrorAsync(response));
    }

    [Fact]
    public async Task UnknownRecipientIsListedInTheError()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);
        var unknown = Guid.NewGuid();

        using var response = await PostAsync(client, token, MessagesPath, Message(characterId) with { recipientIds = [unknown] });

        var error = await ReadAsync(response);
        Assert.Equivalent(
            new { Code = (string?)"errors.unknownCharacters", Ids = (string?[])[unknown.ToString("D")] },
            new
            {
                Code = error.Property("code").GetString(),
                Ids = error.Property("errors").GetProperty("characterIds").EnumerateArray().Select(id => id.GetString()).ToArray()
            },
            strict: true);
    }

    [Fact]
    public async Task ListLinksEachMessageToItsDetails()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);
        var message = Message(characterId);
        (await PostAsync(client, token, MessagesPath, message)).Dispose();

        using var response = await GetAsync(client, token, MessagesPath);

        Assert.Equal([$"{MessagesPath}/{message.messageId:D}"], (await ReadAsync(response)).EntityHrefs());
    }

    [Fact]
    public async Task ListNeverCarriesTheBody()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);
        (await PostAsync(client, token, MessagesPath, Message(characterId))).Dispose();

        using var response = await GetAsync(client, token, MessagesPath);

        Assert.False((await ReadAsync(response)).EntityProperties()[0].TryGetProperty("body", out _));
    }

    [Fact]
    public async Task FilteredListKeepsTheCharacterInItsPageLinks()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);

        using var response = await GetAsync(client, token, $"{MessagesPath}?characterId={characterId:D}");

        Assert.Equal($"{MessagesPath}?characterId={characterId:D}", (await ReadAsync(response)).LinkHref(SirenRelations.First));
    }

    [Fact]
    public async Task ReaderOpensAMessageWithItsBody()
    {
        using var client = factory.CreateClient();
        var (adminToken, readerToken, _) = await BothTokensAsync(client);
        var characterId = await CreateCharacterAsync(client, adminToken);
        var message = Message(characterId);
        (await PostAsync(client, adminToken, MessagesPath, message)).Dispose();

        using var response = await GetAsync(client, readerToken, $"{MessagesPath}/{message.messageId:D}");

        var document = await ReadAsync(response);
        Assert.Equal(
            new { Body = (string?)message.body, Actions = 0 },
            new { Body = document.Property("body").GetString(), Actions = document.ActionNames().Length });
    }

    [Fact]
    public async Task OpeningIsRecordedWithTheReadersAccount()
    {
        using var client = factory.CreateClient();
        var (adminToken, readerToken, readerId) = await BothTokensAsync(client);
        var characterId = await CreateCharacterAsync(client, adminToken);
        var message = Message(characterId);
        (await PostAsync(client, adminToken, MessagesPath, message)).Dispose();

        (await GetAsync(client, readerToken, $"{MessagesPath}/{message.messageId:D}")).Dispose();

        var opened = (await factory.LoadStreamEventsAsync(message.messageId))[^1];
        Assert.Equal(
            new { Type = "message_opened", Actor = (string?)readerId.ToString("D") },
            new { Type = opened.EventTypeName, Actor = opened.UserName });
    }

    [Fact]
    public async Task AdministratorGetsTheReplyActionWithTheSourceFilledIn()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);
        var message = Message(characterId);
        (await PostAsync(client, token, MessagesPath, message)).Dispose();

        using var response = await GetAsync(client, token, $"{MessagesPath}/{message.messageId:D}");

        Assert.Equal(message.messageId.ToString("D"), (await ReadAsync(response)).ActionFieldValue("reply-to-message", "replyToMessageId"));
    }

    [Fact]
    public async Task ReplyLinksToItsSource()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var characterId = await CreateCharacterAsync(client, token);
        var source = Message(characterId);
        (await PostAsync(client, token, MessagesPath, source)).Dispose();

        using var reply = await PostAsync(client, token, MessagesPath,
            Message(characterId) with { title = "Re: Supply run", replyToMessageId = source.messageId });

        Assert.Equal($"{MessagesPath}/{source.messageId:D}", (await ReadAsync(reply)).LinkHref(SirenRelations.ReplySource));
    }

    [Fact]
    public async Task UnknownMessageIsNotFound()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await GetAsync(client, token, $"{MessagesPath}/{Guid.NewGuid():D}");

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.NotFound, "errors.notFound"), await ErrorAsync(response));
    }

    private static MessageRequest Message(Guid characterId) =>
        new(Guid.NewGuid(), characterId, [characterId], "Supply run", "Meet at the dock.\nBring crates.", null);

    private static async Task<Guid> CreateCharacterAsync(HttpClient client, string token)
    {
        using var player = await PostAsync(client, token, PlayersPath, new { displayName = "Ann", type = "player" });
        var playerId = (await ReadAsync(player)).Property("playerId").GetGuid();
        using var character = await PostAsync(client, token, $"{PlayersPath}/{playerId:D}/characters",
            new { name = "Kira", experiencePoints = 0 });
        return (await ReadAsync(character)).Property("characterId").GetGuid();
    }

    private async Task<string> AdministratorTokenAsync(HttpClient client)
    {
        await factory.BootstrapAdministratorAsync();
        return await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");
    }

    private async Task<(string AdministratorToken, string ReaderToken, Guid ReaderId)> BothTokensAsync(HttpClient client)
    {
        var readerId = await factory.CreateReaderAccountAsync(client);
        return (await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1"),
            await AdministratorAuthenticationFactory.SignInReaderAsync(client),
            readerId);
    }

    private async Task<string> ReaderTokenAsync(HttpClient client)
    {
        await factory.CreateReaderAccountAsync(client);
        return await AdministratorAuthenticationFactory.SignInReaderAsync(client);
    }

    private static Task<HttpResponseMessage> GetAsync(HttpClient client, string token, string path) =>
        AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, path, token);

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string token, string path, object body) =>
        AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, path, token, body);

    private static async Task<SirenDocument> ReadAsync(HttpResponseMessage response) =>
        new(JsonDocument.Parse(await response.Content.ReadAsStringAsync(Token)).RootElement.Clone());

    private static async Task<ErrorSnapshot> ErrorAsync(HttpResponseMessage response) =>
        new(response.StatusCode, (await ReadAsync(response)).Property("code").GetString());

    // Lower-case members serialize to the request's JSON field names.
#pragma warning disable IDE1006
    private sealed record MessageRequest(
        Guid messageId,
        Guid senderId,
        Guid[] recipientIds,
        string title,
        string body,
        Guid? replyToMessageId);
#pragma warning restore IDE1006

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

        public string? ActionFieldValue(string action, string field) =>
            Root.GetProperty("actions").EnumerateArray()
                .Single(candidate => candidate.GetProperty("name").GetString() == action)
                .GetProperty("fields").EnumerateArray()
                .Single(candidate => candidate.GetProperty("name").GetString() == field)
                .GetProperty("value").GetString();

        public string?[] EntityHrefs() =>
            Root.GetProperty("entities").EnumerateArray().Select(entity => entity.GetProperty("href").GetString()).ToArray();

        public JsonElement[] EntityProperties() =>
            Root.GetProperty("entities").EnumerateArray().Select(entity => entity.GetProperty("properties")).ToArray();
    }
}
