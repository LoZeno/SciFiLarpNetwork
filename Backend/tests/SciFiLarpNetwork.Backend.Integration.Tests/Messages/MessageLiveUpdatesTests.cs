using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Messages;

/// <summary>
/// Connects with the real SignalR client. The test server has no sockets, so these tests use long polling; the
/// authorization header and the hub's messages are the same for every transport.
/// </summary>
[Collection("Backend database integration")]
public sealed class MessageLiveUpdatesTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private const string MessagesPath = "/local/api/v1/messages";
    private const string LivePath = "/local/api/v1/messages/live";
    private const string PlayersPath = "/local/api/v1/players";
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CollectionLinksToTheLiveUpdates()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, MessagesPath, token);

        var links = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Token)).RootElement.GetProperty("links").EnumerateArray();
        Assert.Contains(links, link =>
            link.GetProperty("rel").EnumerateArray().Any(rel => rel.GetString() == SirenRelations.LiveUpdates) &&
            link.GetProperty("href").GetString() == LivePath);
    }

    [Fact]
    public async Task ReaderHearsEverySentMessageWithItsRecipientsOnly()
    {
        using var client = factory.CreateClient();
        await factory.CreateReaderAccountAsync(client);
        var adminToken = await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");
        var readerToken = await AdministratorAuthenticationFactory.SignInReaderAsync(client);
        var characterId = await CreateCharacterAsync(client, adminToken);
        var heard = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var connection = Connect(readerToken);
        connection.On<JsonElement>("messageSent", notice => heard.TrySetResult(notice));
        await connection.StartAsync(Token);
        var messageId = Guid.NewGuid();

        (await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, MessagesPath, adminToken,
            new { messageId, senderId = characterId, recipientIds = new[] { characterId }, title = "Supply run", body = "Secret text" })).Dispose();

        // The exact text also proves the notice carries neither the title nor the text.
        Assert.Equal($$"""{"messageId":"{{messageId:D}}","recipientIds":["{{characterId:D}}"]}""",
            (await heard.Task.WaitAsync(Patience, Token)).GetRawText());
    }

    [Fact]
    public async Task AnonymousConnectionIsRefused()
    {
        await using var connection = Connect(token: null);

        var failure = await Assert.ThrowsAsync<HttpRequestException>(() => connection.StartAsync(Token));

        Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode);
    }

    [Fact]
    public async Task ConnectionClosesWhenTheSessionExpires()
    {
        // Sessions last eight hours. Signing in with the clock set back leaves the session a few real seconds, so the
        // connection must close whether the expiry is checked against the application clock or the real one.
        using var client = factory.CreateClient();
        await factory.BootstrapAdministratorAsync();
        factory.Clock.Advance(-(TimeSpan.FromHours(8) - TimeSpan.FromSeconds(3)));
        var token = await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var connection = Connect(token);
        connection.Closed += _ =>
        {
            closed.TrySetResult();
            return Task.CompletedTask;
        };

        await connection.StartAsync(Token);

        await closed.Task.WaitAsync(Patience, Token);
    }

    private HubConnection Connect(string? token) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, LivePath), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult(token);
            })
            .Build();

    private static async Task<Guid> CreateCharacterAsync(HttpClient client, string token)
    {
        using var player = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, PlayersPath, token,
            new { displayName = "Ann", type = "player" });
        var playerId = JsonDocument.Parse(await player.Content.ReadAsStringAsync(Token)).RootElement.GetProperty("properties").GetProperty("playerId").GetGuid();
        using var character = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post,
            $"{PlayersPath}/{playerId:D}/characters", token, new { name = "Kira", experiencePoints = 0 });
        return JsonDocument.Parse(await character.Content.ReadAsStringAsync(Token)).RootElement.GetProperty("properties").GetProperty("characterId").GetGuid();
    }

    private async Task<string> AdministratorTokenAsync(HttpClient client)
    {
        await factory.BootstrapAdministratorAsync();
        return await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");
    }
}
