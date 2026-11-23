using System.Net;
using System.Text.Json;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Api.Messages;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Messages;

/// <summary>In-memory messages and characters that answer with the backend's Siren shapes.</summary>
internal sealed class FakeMessageApi : IManagementApiClient
{
    public const string MessagesHref = "/api/v1/messages";
    public const string CharactersHref = "/api/v1/characters";
    public const string LiveHref = "/api/v1/messages/live";
    public static readonly Guid KiraId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid OdoId = new("22222222-2222-2222-2222-222222222222");
    public static readonly Guid JadeId = new("33333333-3333-3333-3333-333333333333");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public bool CanWrite { get; init; } = true;

    public int MaximumRecipients { get; init; } = 200;

    public List<FakeMessage> Messages { get; } = [];

    public Dictionary<Guid, (int Number, string Name)> Characters { get; } = new()
    {
        [KiraId] = (1, "Kira"),
        [OdoId] = (2, "Odo"),
        [JadeId] = (3, "Jade")
    };

    public HashSet<Guid> ArchivedCharacters { get; } = [];

    public List<Submission> Submissions { get; } = [];

    /// <summary>Every message details address the screen opened, in order.</summary>
    public List<string> Openings { get; } = [];

    /// <summary>Every list address the screen loaded, in order.</summary>
    public List<string> ListLoads { get; } = [];

    /// <summary>The live updates address the screen listens to; <see langword="null"/> when it is not listening.</summary>
    public string? ListeningTo { get; private set; }

    /// <summary>When set, listening to live updates fails with it.</summary>
    public Exception? ListenFailure { get; init; }

    private Func<MessageSentNotice, Task>? _onMessageSent;
    private Func<Task>? _onReconnected;

    /// <summary>Plays the server announcing a sent message to the listening screen.</summary>
    public Task AnnounceAsync(FakeMessage message) =>
        _onMessageSent?.Invoke(new MessageSentNotice(message.Id, message.RecipientIds)) ?? Task.CompletedTask;

    /// <summary>Plays the live connection coming back after a network failure.</summary>
    public Task ReconnectAsync() => _onReconnected?.Invoke() ?? Task.CompletedTask;

    public Task<IAsyncDisposable> ListenForMessagesAsync(string apiBaseUrl, string href, Func<MessageSentNotice, Task> onMessageSent,
        Func<Task> onReconnected, CancellationToken cancellationToken = default)
    {
        if (ListenFailure is { } failure)
        {
            return Task.FromException<IAsyncDisposable>(failure);
        }

        ListeningTo = href;
        _onMessageSent = onMessageSent;
        _onReconnected = onReconnected;
        return Task.FromResult<IAsyncDisposable>(new Listening(this));
    }

    /// <summary>One-shot failures for the next submission of the named action.</summary>
    public Dictionary<string, Exception> Failures { get; } = [];

    public FakeMessage AddMessage(string title, Guid senderId, params Guid[] recipientIds)
    {
        var added = new FakeMessage(Guid.NewGuid(), senderId, recipientIds, title, $"Body of {title}",
            new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero).AddMinutes(Messages.Count), null);
        Messages.Add(added);
        return added;
    }

    public Task<SirenEntity> GetResourceAsync(string apiBaseUrl, string href, string locale, CancellationToken cancellationToken = default)
    {
        var (path, query) = Split(href);
        if (path == MessagesHref)
        {
            ListLoads.Add(href);
            return Task.FromResult(Collection(query));
        }

        if (path == CharactersHref)
        {
            return Task.FromResult(CharacterCollection(query));
        }

        Openings.Add(href);
        var found = Messages.FirstOrDefault(message => path == Href(message));
        return found is null
            ? Task.FromException<SirenEntity>(new ManagementApiException("errors.notFound", HttpStatusCode.NotFound))
            : Task.FromResult(Details(found));
    }

    public Task<SirenEntity> SubmitActionAsync(string apiBaseUrl, SirenAction action, JsonElement? body, string locale,
        CancellationToken cancellationToken = default)
    {
        Submissions.Add(new Submission(action.Name!, body?.GetRawText()));
        if (Failures.Remove(action.Name!, out var failure))
        {
            return Task.FromException<SirenEntity>(failure);
        }

        var request = body!.Value;
        var messageId = request.GetProperty("messageId").GetGuid();
        var existing = Messages.FirstOrDefault(message => message.Id == messageId);
        if (existing is not null)
        {
            return Task.FromResult(Details(existing));
        }

        var sent = new FakeMessage(
            messageId,
            request.GetProperty("senderId").GetGuid(),
            request.GetProperty("recipientIds").EnumerateArray().Select(id => id.GetGuid()).ToArray(),
            request.GetProperty("title").GetString()!,
            request.GetProperty("body").GetString()!,
            new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero),
            request.GetProperty("replyToMessageId").ValueKind == JsonValueKind.Null ? null : request.GetProperty("replyToMessageId").GetGuid());
        Messages.Add(sent);
        return Task.FromResult(Details(sent));
    }

    public Task<byte[]> DownloadActionAsync(string apiBaseUrl, SirenAction action, JsonElement body, string mediaType, string locale,
        CancellationToken cancellationToken = default) =>
        Task.FromException<byte[]>(new NotSupportedException("This fake serves no files."));

    public Task<byte[]> GetImageAsync(string apiBaseUrl, string href, string locale, CancellationToken cancellationToken = default) =>
        Task.FromException<byte[]>(new NotSupportedException("This fake serves no images."));

    public Task<byte[]> GetFileAsync(string apiBaseUrl, string href, string mediaType, string locale,
        CancellationToken cancellationToken = default) =>
        Task.FromException<byte[]>(new NotSupportedException("This fake serves no files."));

    public Task<ManagementSession> SignInAsync(string apiBaseUrl, string userName, string password, string locale,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ManagementSession(userName, "administrator", DateTimeOffset.UtcNow.AddHours(1)));

    public Task SignOutAsync(string apiBaseUrl, string locale, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<ManagementSession> RenewSessionAsync(string apiBaseUrl, string locale, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ManagementSession("operator", "administrator", DateTimeOffset.UtcNow.AddHours(1)));

    public Task<ApiDiscovery> DiscoverAsync(string apiBaseUrl, string locale, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ApiDiscovery(
            new SirenEntity { Class = ["api"], Properties = Json(new { title = "Administration" }) },
            [new SirenLink { Rel = [SirenRelations.Navigation, SirenRelations.Messages], Href = MessagesHref, Title = "Messages" }]));

    /// <summary>The list newest first, 50 per page, of every message or of one character's incoming messages.</summary>
    private SirenEntity Collection(string query)
    {
        var parameters = query.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(pair => pair[0], pair => Uri.UnescapeDataString(pair[1]));
        Guid? characterId = parameters.TryGetValue("characterId", out var id) ? Guid.Parse(id) : null;
        var page = parameters.TryGetValue("page", out var number) ? int.Parse(number, System.Globalization.CultureInfo.InvariantCulture) : 1;
        var listed = Messages
            .Where(message => characterId is not { } character || message.RecipientIds.Contains(character))
            .OrderByDescending(message => message.SentAtUtc)
            .ToArray();
        var pageCount = Math.Max(1, (listed.Length + 49) / 50);
        var links = new List<SirenLink>
        {
            new() { Rel = ["self"], Href = PageHref(characterId, page) },
            new() { Rel = [SirenRelations.Characters], Href = CharactersHref },
            new() { Rel = [SirenRelations.LiveUpdates], Href = LiveHref }
        };
        if (page > 1)
        {
            links.Add(new SirenLink { Rel = [SirenRelations.Previous], Href = PageHref(characterId, page - 1) });
        }

        if (page < pageCount)
        {
            links.Add(new SirenLink { Rel = [SirenRelations.Next], Href = PageHref(characterId, page + 1) });
        }

        return new SirenEntity
        {
            Class = ["message-collection"],
            Properties = Json(new
            {
                title = "Messages",
                description = "In-game messages",
                characterId,
                pageNumber = page,
                pageCount,
                totalCount = listed.Length,
                maximumRecipients = MaximumRecipients,
                maximumTitleLength = 128,
                maximumBodyLength = 10000
            }),
            Entities = listed.Skip((page - 1) * 50).Take(50).Select(message => new SirenSubEntity
            {
                Rel = ["item"],
                Class = ["message"],
                Href = Href(message),
                Properties = Json(new
                {
                    messageId = message.Id,
                    sender = Character(message.SenderId),
                    recipientCount = message.RecipientIds.Count,
                    title = message.Title,
                    sentAtUtc = message.SentAtUtc,
                    replyToMessageId = message.ReplyToMessageId
                })
            }).ToArray(),
            Links = links,
            Actions =
            [
                new SirenAction
                {
                    Name = "filter-messages",
                    Href = MessagesHref,
                    Method = "GET",
                    Fields = [new SirenField { Name = "characterId", Type = "hidden" }]
                },
                .. CanWrite ? [SendAction("send-message")] : Array.Empty<SirenAction>()
            ]
        };
    }

    private SirenEntity Details(FakeMessage message)
    {
        var source = message.ReplyToMessageId is { } sourceId ? Messages.FirstOrDefault(candidate => candidate.Id == sourceId) : null;
        var links = new List<SirenLink>
        {
            new() { Rel = ["self"], Href = Href(message) },
            new() { Rel = ["collection"], Href = MessagesHref }
        };
        if (source is not null)
        {
            links.Add(new SirenLink { Rel = [SirenRelations.ReplySource], Href = Href(source) });
        }

        return new SirenEntity
        {
            Class = ["message"],
            Properties = Json(new
            {
                messageId = message.Id,
                sender = Character(message.SenderId),
                recipients = message.RecipientIds.Select(Character).ToArray(),
                title = message.Title,
                body = message.Body,
                sentAtUtc = message.SentAtUtc,
                sentBy = "local-admin",
                replyTo = source is null ? null : new { messageId = source.Id, title = source.Title }
            }),
            Links = links,
            Actions = CanWrite ? [SendAction("reply-to-message")] : null
        };
    }

    private object Character(Guid characterId) =>
        Characters.TryGetValue(characterId, out var character)
            ? new { characterId, isKnown = true, number = (int?)character.Number, name = (string?)character.Name, isArchived = ArchivedCharacters.Contains(characterId) }
            : new { characterId, isKnown = false, number = (int?)null, name = (string?)null, isArchived = false };

    private SirenEntity CharacterCollection(string query)
    {
        var search = Uri.UnescapeDataString(query.StartsWith("search=", StringComparison.Ordinal) ? query["search=".Length..] : string.Empty);
        return new SirenEntity
        {
            Class = ["character-collection"],
            Properties = Json(new { title = "Characters" }),
            Entities = Characters
                .Where(character => search.Length > 0 && character.Value.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .Select(character => new SirenSubEntity
                {
                    Rel = ["item"],
                    Href = $"{CharactersHref}/{character.Key:D}",
                    Properties = Json(new
                    {
                        characterId = character.Key,
                        number = character.Value.Number,
                        playerId = Guid.Empty,
                        playerDisplayName = "Ann",
                        name = character.Value.Name,
                        experiencePoints = 0,
                        status = "active",
                        qrIdentifier = "qr",
                        stats = Array.Empty<object>(),
                        isArchived = false,
                        version = 1
                    })
                })
                .ToArray(),
            Actions =
            [
                new SirenAction
                {
                    Name = "search-characters",
                    Href = CharactersHref,
                    Method = "GET",
                    Fields = [new SirenField { Name = "search", Type = "text" }]
                }
            ]
        };
    }

    private static SirenAction SendAction(string name) =>
        new() { Name = name, Href = MessagesHref, Method = "POST", Type = "application/json" };

    private static string PageHref(Guid? characterId, int page)
    {
        var parameters = new List<string>();
        if (characterId is { } character)
        {
            parameters.Add($"characterId={character:D}");
        }

        if (page > 1)
        {
            parameters.Add($"page={page}");
        }

        return parameters.Count == 0 ? MessagesHref : $"{MessagesHref}?{string.Join('&', parameters)}";
    }

    private static string Href(FakeMessage message) => $"{MessagesHref}/{message.Id:D}";

    private static (string Path, string Query) Split(string href)
    {
        var index = href.IndexOf('?', StringComparison.Ordinal);
        return index < 0 ? (href, string.Empty) : (href[..index], href[(index + 1)..]);
    }

    private static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value, JsonOptions);

    private sealed class Listening(FakeMessageApi api) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            api.ListeningTo = null;
            api._onMessageSent = null;
            api._onReconnected = null;
            return ValueTask.CompletedTask;
        }
    }
}
