using System.Globalization;
using System.Net;
using System.Text.Json;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.LarpEvents;

/// <summary>In-memory events, days, registrations, and characters that answer with the backend's Siren shapes.</summary>
internal sealed class FakeLarpEventApi : IManagementApiClient
{
    public const string EventsHref = "/api/v1/events";
    public const string CharactersHref = "/api/v1/characters";
    public static readonly byte[] CardsPdf = "%PDF-cards"u8.ToArray();
    public static readonly Guid KiraId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid OdoId = new("22222222-2222-2222-2222-222222222222");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public bool CanWrite { get; init; } = true;

    public List<FakeEvent> Events { get; } = [];

    public Dictionary<Guid, (int Number, string Name, int Experience)> Characters { get; } = new()
    {
        [KiraId] = (1, "Kira", 10),
        [OdoId] = (2, "Odo", 4)
    };

    /// <summary>Characters shown as Retired in participant lists.</summary>
    public HashSet<Guid> InactiveCharacters { get; } = [];

    public List<Submission> Submissions { get; } = [];

    /// <summary>One-shot failures for the next submission of the named action.</summary>
    public Dictionary<string, Exception> Failures { get; } = [];

    public FakeEvent AddEvent(string name, bool isOpen = true, params DateOnly[] dates)
    {
        var added = new FakeEvent(Guid.NewGuid(), Events.Count + 1, name)
        {
            IsOpen = isOpen,
            Days = dates.Select(date => (Guid.NewGuid(), date, (int?)null)).ToList()
        };
        Events.Add(added);
        return added;
    }

    public Task<SirenEntity> GetResourceAsync(string apiBaseUrl, string href, string locale, CancellationToken cancellationToken = default)
    {
        var (path, query) = Split(href);
        if (path == EventsHref)
        {
            return Task.FromResult(Collection(query.Contains("includeArchived=true", StringComparison.Ordinal)));
        }

        if (path == CharactersHref)
        {
            return Task.FromResult(CharacterCollection(query));
        }

        var found = Events.FirstOrDefault(item => path.StartsWith(Href(item), StringComparison.Ordinal));
        return found is null
            ? Task.FromException<SirenEntity>(new ManagementApiException("errors.notFound", HttpStatusCode.NotFound))
            : Task.FromResult(path.EndsWith("/participants", StringComparison.Ordinal)
                ? ParticipantCollection(found, query)
                : Event(found));
    }

    public Task<SirenEntity> SubmitActionAsync(string apiBaseUrl, SirenAction action, JsonElement? body, string locale,
        CancellationToken cancellationToken = default)
    {
        Submissions.Add(new Submission(action.Name!, body?.GetRawText()));
        if (Failures.Remove(action.Name!, out var failure))
        {
            return Task.FromException<SirenEntity>(failure);
        }

        if (action.Name == "create-event")
        {
            var created = AddEvent(body!.Value.GetProperty("name").GetString()!);
            return Task.FromResult(Event(created));
        }

        var target = Events.Single(item => action.Href!.StartsWith(Href(item), StringComparison.Ordinal));
        Apply(target, action.Name!, body);
        return Task.FromResult(Event(target));
    }

    public Task<byte[]> DownloadActionAsync(string apiBaseUrl, SirenAction action, JsonElement body, string mediaType, string locale,
        CancellationToken cancellationToken = default)
    {
        Submissions.Add(new Submission(action.Name!, body.GetRawText()));
        return Task.FromResult(CardsPdf);
    }

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
            [new SirenLink { Rel = [SirenRelations.Navigation, SirenRelations.Events], Href = EventsHref, Title = "Events" }]));

    private static void Apply(FakeEvent target, string actionName, JsonElement? body)
    {
        switch (actionName)
        {
            case "update-event":
                target.Name = body!.Value.GetProperty("name").GetString()!;
                target.BaseExperience = OptionalInt(body.Value, "baseExperience");
                break;
            case "close-event":
                target.IsOpen = false;
                foreach (var award in body!.Value.GetProperty("awards").EnumerateArray())
                {
                    var awarded = award.GetProperty("characterId").GetGuid();
                    target.BaseGiven.UnionWith(award.GetProperty("baseExperience").GetBoolean() ? [awarded] : []);
                    target.GivenDays[awarded] = [.. target.GivenDays.GetValueOrDefault(awarded) ?? [], .. DayIds(award)];
                }

                break;
            case "reopen-event":
                target.IsOpen = true;
                break;
            case "archive-event":
                target.IsArchived = true;
                break;
            case "unarchive-event":
                target.IsArchived = false;
                break;
            case "add-day":
                target.Days.Add((Guid.NewGuid(), Date(body!.Value), OptionalInt(body.Value, "experience")));
                break;
            case "update-day":
                var dayId = body!.Value.GetProperty("dayId").GetGuid();
                target.Days[target.Days.FindIndex(day => day.Id == dayId)] = (dayId, Date(body.Value), OptionalInt(body.Value, "experience"));
                break;
            case "remove-day":
                var removed = body!.Value.GetProperty("dayId").GetGuid();
                target.Days.RemoveAll(day => day.Id == removed);
                break;
            case "register-participant":
                var registered = target.Registrations.GetValueOrDefault(Character(body!.Value)) ?? [];
                target.Registrations[Character(body.Value)] = [.. registered.Union(DayIds(body.Value))];
                break;
            case "unregister-participant":
                var remaining = target.Registrations[Character(body!.Value)].Except(DayIds(body.Value)).ToList();
                if (remaining.Count == 0)
                {
                    target.Registrations.Remove(Character(body.Value));
                }
                else
                {
                    target.Registrations[Character(body.Value)] = remaining;
                }

                break;
            case "change-experience":
                target.Experience[Character(body!.Value)] =
                    target.Experience.GetValueOrDefault(Character(body.Value)) + body.Value.GetProperty("amount").GetInt32();
                break;
            default:
                throw new InvalidOperationException($"Unexpected action {actionName}.");
        }
    }

    private SirenEntity Collection(bool includeArchived) =>
        new()
        {
            Class = ["event-collection"],
            Properties = Json(new
            {
                title = "Events",
                description = "",
                includesArchived = includeArchived,
                pageNumber = 1,
                pageCount = 1,
                totalCount = Events.Count,
                statuses = new[] { new { name = "open", title = "Open" }, new { name = "closed", title = "Closed" } }
            }),
            Entities = Events
                .Where(item => includeArchived || !item.IsArchived)
                .Select(item => new SirenSubEntity { Rel = ["item"], Href = Href(item), Properties = EventProperties(item) })
                .ToArray(),
            Links =
            [
                new SirenLink { Rel = ["self"], Href = includeArchived ? $"{EventsHref}?includeArchived=true" : EventsHref },
                new SirenLink { Rel = [SirenRelations.WithArchived], Href = $"{EventsHref}?includeArchived=true" }
            ],
            Actions = CanWrite ? [Action("create-event", EventsHref, "POST")] : null
        };

    private SirenEntity Event(FakeEvent item)
    {
        var href = Href(item);
        List<SirenAction> actions =
        [
            new SirenAction
            {
                Name = "list-participants",
                Href = $"{href}/participants",
                Method = "GET",
                Fields = [new SirenField { Name = "dayId", Type = "hidden" }]
            },
            Action("print-event-cards", $"{href}/cards", "POST")
        ];
        if (CanWrite)
        {
            actions.AddRange(item.IsOpen
                ?
                [
                    Action("update-event", href, "PUT"),
                    Action("add-day", $"{href}/days", "POST"),
                    Action("update-day", $"{href}/days/update", "POST"),
                    Action("remove-day", $"{href}/days/remove", "POST"),
                    Action("register-participant", $"{href}/participants", "POST"),
                    Action("unregister-participant", $"{href}/participants/unregister", "POST"),
                    Action("change-experience", $"{href}/participants/experience", "POST"),
                    Action("close-event", $"{href}/close", "POST")
                ]
                : item.IsArchived
                    ? [Action("unarchive-event", $"{href}/unarchive", "POST")]
                    : [Action("reopen-event", $"{href}/reopen", "POST"), Action("archive-event", $"{href}/archive", "POST")]);
        }

        return new SirenEntity
        {
            Class = ["event"],
            Properties = EventProperties(item),
            Links =
            [
                new SirenLink { Rel = ["self"], Href = href },
                new SirenLink { Rel = [SirenRelations.Characters], Href = CharactersHref }
            ],
            Actions = actions
        };
    }

    private static JsonElement EventProperties(FakeEvent item)
    {
        var days = item.Days.OrderBy(day => day.Date).ToArray();
        return Json(new
        {
            eventId = item.Id,
            number = item.Number,
            name = item.Name,
            description = (string?)null,
            status = item.IsOpen ? "open" : "closed",
            isArchived = item.IsArchived,
            baseExperience = item.BaseExperience,
            firstDate = days.Length == 0 ? (DateOnly?)null : days[0].Date,
            lastDate = days.Length == 0 ? (DateOnly?)null : days[^1].Date,
            days = days.Select(day => new
            {
                dayId = day.Id,
                date = day.Date,
                experience = day.Experience,
                participantCount = item.Registrations.Count(registration => registration.Value.Contains(day.Id))
            }),
            participantCount = item.Registrations.Count,
            version = 1
        });
    }

    private SirenEntity ParticipantCollection(FakeEvent item, string query)
    {
        var dayId = query.StartsWith("dayId=", StringComparison.Ordinal) ? Guid.Parse(query["dayId=".Length..]) : (Guid?)null;
        return new SirenEntity
        {
            Class = ["participant-collection"],
            Properties = Json(new { title = "Participants", eventId = item.Id, dayId, count = item.Registrations.Count }),
            Entities = item.Registrations
                .Where(registration => dayId is not { } day || registration.Value.Contains(day))
                .OrderBy(registration => Characters[registration.Key].Number)
                .Select(registration => new SirenSubEntity
                {
                    Rel = ["item"],
                    Href = $"{CharactersHref}/{registration.Key:D}",
                    Properties = Json(new
                    {
                        characterId = registration.Key,
                        characterNumber = Characters[registration.Key].Number,
                        characterName = Characters[registration.Key].Name,
                        playerId = Guid.Empty,
                        playerDisplayName = "Ann",
                        status = InactiveCharacters.Contains(registration.Key) ? "retired" : "active",
                        isArchived = false,
                        experiencePoints = Characters[registration.Key].Experience,
                        dayIds = registration.Value,
                        dates = item.Days.Where(day => registration.Value.Contains(day.Id)).Select(day => day.Date).Order(),
                        experienceTotal = item.Experience.GetValueOrDefault(registration.Key),
                        experienceChanges = item.Experience.TryGetValue(registration.Key, out var total)
                            ? new[] { new { amount = total, changedBy = "local-admin", changedAtUtc = new DateTimeOffset(2026, 5, 2, 18, 0, 0, TimeSpan.Zero), isClosingAward = false } }
                            : [],
                        baseExperienceGiven = item.BaseGiven.Contains(registration.Key),
                        givenDayIds = item.GivenDays.GetValueOrDefault(registration.Key) ?? []
                    })
                })
                .ToArray()
        };
    }

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
                        experiencePoints = character.Value.Experience,
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
                    Fields = [new SirenField { Name = "search" }, new SirenField { Name = "includeArchived" }]
                }
            ]
        };
    }

    private static (string Path, string Query) Split(string href)
    {
        var separator = href.IndexOf('?', StringComparison.Ordinal);
        return separator < 0 ? (href, string.Empty) : (href[..separator], href[(separator + 1)..]);
    }

    private static string Href(FakeEvent item) => $"{EventsHref}/{item.Id:D}";

    private static DateOnly Date(JsonElement body) =>
        DateOnly.ParseExact(body.GetProperty("date").GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static int? OptionalInt(JsonElement body, string name) =>
        body.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : null;

    private static Guid Character(JsonElement body) => body.GetProperty("characterId").GetGuid();

    private static IEnumerable<Guid> DayIds(JsonElement body) =>
        body.GetProperty("dayIds").EnumerateArray().Select(day => day.GetGuid());

    private static SirenAction Action(string name, string href, string method) =>
        new() { Name = name, Href = href, Method = method, Type = "application/json" };

    private static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value, JsonOptions);

    internal sealed class FakeEvent(Guid id, int number, string name)
    {
        public Guid Id { get; } = id;

        public int Number { get; } = number;

        public string Name { get; set; } = name;

        public bool IsOpen { get; set; } = true;

        public bool IsArchived { get; set; }

        public int? BaseExperience { get; set; }

        public List<(Guid Id, DateOnly Date, int? Experience)> Days { get; init; } = [];

        public HashSet<Guid> BaseGiven { get; } = [];

        public Dictionary<Guid, List<Guid>> GivenDays { get; } = [];

        public Dictionary<Guid, List<Guid>> Registrations { get; } = [];

        public Dictionary<Guid, int> Experience { get; } = [];
    }
}
