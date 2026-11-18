using System.Net;
using System.Text.Json;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Players;

/// <summary>
/// An in-memory players, characters, stats, and networks API that answers with the same Siren shape, links, and
/// actions as the backend.
/// </summary>
internal sealed class FakePlayerApi : IManagementApiClient
{
    public const string PlayersHref = "/api/v1/players";
    private const string CharactersHref = "/api/v1/characters";
    private const string StatsHref = "/api/v1/character-stats";
    private const string NetworksHref = "/api/v1/networks";
    private const string TalentsHref = "/api/v1/talents";
    private const string SkillsHref = "/api/v1/skills";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public bool CanWrite { get; init; } = true;

    public int PageSize { get; init; } = 50;

    public int TalentLimit { get; init; } = 5;

    public List<FakePlayer> Players { get; } = [];

    public List<FakeCharacter> Characters { get; } = [];

    public List<FakeStat> Stats { get; } = [];

    public List<FakeRosterNetwork> Networks { get; } = [];

    public List<FakeRosterTalent> Talents { get; } = [];

    public List<FakeRosterSkill> Skills { get; } = [];

    public FakeRosterTalent AddTalent(string name, bool isArchived = false)
    {
        var talent = new FakeRosterTalent(Guid.NewGuid(), name, isArchived);
        Talents.Add(talent);
        return talent;
    }

    public FakeRosterSkill AddSkill(string name, int cost, FakeRosterTalent talent, bool isMultiPurchase = false, bool isArchived = false)
    {
        var skill = new FakeRosterSkill(Guid.NewGuid(), name, cost, talent.Id, isMultiPurchase, isArchived);
        Skills.Add(skill);
        return skill;
    }

    public List<Submission> Submissions { get; } = [];

    public List<string> Gets { get; } = [];

    /// <summary>One-shot failures for the next submission of the named action.</summary>
    public Dictionary<string, Exception> SubmitFailures { get; } = [];

    public FakePlayer AddPlayer(string name, bool isActive = true)
    {
        var player = new FakePlayer(Guid.NewGuid(), Players.Count + 1, name) { IsActive = isActive };
        Players.Add(player);
        return player;
    }

    public FakeCharacter AddCharacter(FakePlayer player, string name, string status = "active")
    {
        var character = new FakeCharacter(Guid.NewGuid(), Characters.Count + 1, player.Id, name) { Status = status };
        Characters.Add(character);
        return character;
    }

    public FakeStat AddStat(string name, string type, bool isArchived = false)
    {
        var stat = new FakeStat(Guid.NewGuid(), name, type) { IsArchived = isArchived };
        Stats.Add(stat);
        return stat;
    }

    public FakeRosterNetwork AddNetwork(string name, bool isEnabled = true, bool isDefault = false)
    {
        var network = new FakeRosterNetwork(Guid.NewGuid(), name, isEnabled, isDefault);
        Networks.Add(network);
        return network;
    }

    public Task<SirenEntity> GetResourceAsync(string apiBaseUrl, string href, string locale, CancellationToken cancellationToken = default)
    {
        Gets.Add(href);
        var parts = href.Split('?');
        var query = Query(parts.Length > 1 ? parts[1] : string.Empty);
        var segments = parts[0].Split('/', StringSplitOptions.RemoveEmptyEntries);
        try
        {
            return Task.FromResult(segments switch
            {
                ["api", "v1", "players"] => PlayerCollection(query),
                ["api", "v1", "players", var id] => PlayerEntity(FindPlayer(id), query.ContainsKey("includeArchived")),
                ["api", "v1", "characters"] => CharacterCollection(query),
                ["api", "v1", "characters", "by-qr"] => CharacterEntity(FindByQr(query.GetValueOrDefault("identifier"))),
                ["api", "v1", "characters", var id] => CharacterEntity(FindCharacter(id)),
                ["api", "v1", "characters", var id, "skills"] => CharacterSkillsEntity(FindCharacter(id)),
                ["api", "v1", "talents"] => CatalogCollection(TalentsHref, query.ContainsKey("includeArchived"),
                    Talents.Where(talent => query.ContainsKey("includeArchived") || !talent.IsArchived).Select(TalentProperties)),
                ["api", "v1", "skills"] => CatalogCollection(SkillsHref, query.ContainsKey("includeArchived"),
                    Skills.Where(skill => query.ContainsKey("includeArchived") || !skill.IsArchived).Select(SkillProperties)),
                ["api", "v1", "character-stats"] => StatCollection(query.ContainsKey("includeArchived")),
                ["api", "v1", "character-stats", var id] => StatEntity(FindStat(id)),
                ["api", "v1", "networks"] => NetworkCollection(query.ContainsKey("includeDisabled")),
                _ => throw NotFound()
            });
        }
        catch (ManagementApiException failure)
        {
            return Task.FromException<SirenEntity>(failure);
        }
    }

    public Task<byte[]> GetImageAsync(string apiBaseUrl, string href, string locale, CancellationToken cancellationToken = default) =>
        Task.FromException<byte[]>(NotFound());

    public Task<SirenEntity> SubmitActionAsync(
        string apiBaseUrl,
        SirenAction action,
        JsonElement? body,
        string locale,
        CancellationToken cancellationToken = default)
    {
        Submissions.Add(new Submission(action.Name, body?.GetRawText()));
        if (SubmitFailures.Remove(action.Name, out var failure))
        {
            return Task.FromException<SirenEntity>(failure);
        }

        try
        {
            return Task.FromResult(Apply(action, body));
        }
        catch (ManagementApiException apiFailure)
        {
            return Task.FromException<SirenEntity>(apiFailure);
        }
    }

    public Task<ManagementSession> SignInAsync(
        string apiBaseUrl,
        string userName,
        string password,
        string locale,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ManagementSession(userName, "administrator", DateTimeOffset.UtcNow.AddHours(1)));

    public Task SignOutAsync(string apiBaseUrl, string locale, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<ManagementSession> RenewSessionAsync(string apiBaseUrl, string locale, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ManagementSession("operator", "administrator", DateTimeOffset.UtcNow.AddHours(1)));

    public Task<ApiDiscovery> DiscoverAsync(string apiBaseUrl, string locale, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ApiDiscovery(
            new SirenEntity { Class = ["api"], Properties = Json(new { title = "Administration" }) },
            [new SirenLink { Rel = [SirenRelations.Navigation, SirenRelations.Players], Href = PlayersHref, Title = "Players" }]));

    private SirenEntity Apply(SirenAction action, JsonElement? body)
    {
        var segments = action.Href.Split('/', StringSplitOptions.RemoveEmptyEntries);
        switch (action.Name)
        {
            case "create-player":
                var created = AddPlayer(body!.Value.GetProperty("displayName").GetString()!);
                return PlayerEntity(created, false);
            case "update-player":
                var player = FindPlayer(segments[3]);
                RequireVersion(body!.Value, player.Version);
                player.Name = body.Value.GetProperty("displayName").GetString()!;
                player.Version++;
                return PlayerEntity(player, false);
            case "deactivate-player":
                var deactivated = FindPlayer(segments[3]);
                deactivated.IsActive = false;
                foreach (var character in Characters.Where(item => item.PlayerId == deactivated.Id && item.Status == "active"))
                {
                    character.Status = "retired";
                }

                return PlayerEntity(deactivated, false);
            case "activate-player":
                var activated = FindPlayer(segments[3]);
                activated.IsActive = true;
                return PlayerEntity(activated, false);
            case "archive-player" or "unarchive-player":
                var archivedPlayer = FindPlayer(segments[3]);
                archivedPlayer.IsArchived = action.Name == "archive-player";
                return PlayerEntity(archivedPlayer, false);
            case "create-character":
                var owner = FindPlayer(segments[3]);
                var newCharacter = AddCharacter(owner, body!.Value.GetProperty("name").GetString()!);
                return CharacterEntity(newCharacter);
            case "update-character":
                var edited = FindCharacter(segments[3]);
                RequireVersion(body!.Value, edited.Version);
                edited.Name = body.Value.GetProperty("name").GetString()!;
                edited.Status = body.Value.GetProperty("status").GetString()!;
                edited.Version++;
                return CharacterEntity(edited);
            case "archive-character" or "unarchive-character":
                var archivedCharacter = FindCharacter(segments[3]);
                archivedCharacter.IsArchived = action.Name == "archive-character";
                return CharacterEntity(archivedCharacter);
            case "regenerate-qr-identifier":
                var regenerated = FindCharacter(segments[3]);
                regenerated.QrIdentifier = Guid.NewGuid().ToString("N");
                return CharacterEntity(regenerated);
            case "save-character-skills":
                var skillOwner = FindCharacter(segments[3]);
                RequireVersion(body!.Value, skillOwner.Version);
                skillOwner.TalentIds = body.Value.GetProperty("talentIds").EnumerateArray().Select(id => id.GetGuid()).ToList();
                skillOwner.Purchases = body.Value.GetProperty("skills").EnumerateArray()
                    .Select(skill => new FakePurchase(
                        skill.GetProperty("skillId").GetGuid(),
                        skill.GetProperty("talentId").GetGuid(),
                        skill.GetProperty("count").GetInt32(),
                        skill.GetProperty("specifics").GetString()))
                    .ToList();
                skillOwner.Version++;
                return CharacterSkillsEntity(skillOwner);
            case "rename-character-stat":
                var renamed = FindStat(segments[3]);
                RequireVersion(body!.Value, renamed.Version);
                renamed.Name = body.Value.GetProperty("name").GetString()!;
                renamed.Version++;
                return StatEntity(renamed);
            case "archive-character-stat" or "unarchive-character-stat":
                var archivedStat = FindStat(segments[3]);
                archivedStat.IsArchived = action.Name == "archive-character-stat";
                return StatEntity(archivedStat);
            default:
                throw NotFound();
        }
    }

    private SirenEntity PlayerCollection(Dictionary<string, string> query)
    {
        var search = query.GetValueOrDefault("search");
        var includeArchived = query.ContainsKey("includeArchived");
        var matches = Players
            .Where(player => includeArchived || !player.IsArchived)
            .Where(player => search is null || player.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return Page(PlayersHref, "player-collection", query, matches, player => new SirenSubEntity
        {
            Rel = ["item"],
            Href = $"{PlayersHref}/{player.Id:D}",
            Properties = PlayerProperties(player)
        }, playerCollection: true);
    }

    private SirenEntity CharacterCollection(Dictionary<string, string> query)
    {
        var search = query.GetValueOrDefault("search");
        var includeArchived = query.ContainsKey("includeArchived");
        var matches = Characters
            .Where(character => includeArchived || !character.IsArchived)
            .Where(character => search is null || character.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return Page(CharactersHref, "character-collection", query, matches, CharacterItem, playerCollection: false);
    }

    private SirenEntity Page<T>(
        string path,
        string className,
        Dictionary<string, string> query,
        T[] matches,
        Func<T, SirenSubEntity> toItem,
        bool playerCollection)
    {
        var pageNumber = query.TryGetValue("page", out var page) ? int.Parse(page, System.Globalization.CultureInfo.InvariantCulture) : 1;
        var pageCount = Math.Max(1, (matches.Length + PageSize - 1) / PageSize);
        var links = new List<SirenLink> { new() { Rel = ["self"], Href = PageHref(path, query, pageNumber) } };
        if (pageNumber > 1)
        {
            links.Add(new SirenLink { Rel = [SirenRelations.Previous], Href = PageHref(path, query, pageNumber - 1) });
        }

        if (pageNumber < pageCount)
        {
            links.Add(new SirenLink { Rel = [SirenRelations.Next], Href = PageHref(path, query, pageNumber + 1) });
        }

        if (playerCollection)
        {
            links.Add(new SirenLink { Rel = [SirenRelations.CharacterStats], Href = StatsHref });
        }

        var searchFields = new[] { new SirenField { Name = "search" }, new SirenField { Name = "includeArchived" } };
        var actions = new List<SirenAction>
        {
            new() { Name = playerCollection ? "search-players" : "search-characters", Href = path, Method = "GET", Fields = searchFields }
        };
        if (playerCollection)
        {
            actions.Add(new SirenAction { Name = "search-characters", Href = CharactersHref, Method = "GET", Fields = searchFields });
            actions.Add(new SirenAction
            {
                Name = "find-character-by-qr",
                Href = $"{CharactersHref}/by-qr",
                Method = "GET",
                Fields = [new SirenField { Name = "identifier" }]
            });
            if (CanWrite)
            {
                actions.Add(Action("create-player", PlayersHref, "POST"));
            }
        }

        return new SirenEntity
        {
            Class = [className],
            Properties = Json(new
            {
                title = "Players",
                description = "Players",
                search = query.GetValueOrDefault("search"),
                includesArchived = query.ContainsKey("includeArchived"),
                pageNumber,
                pageCount,
                totalCount = matches.Length,
                playerTypes = new[] { new { name = "player", title = "Player" }, new { name = "gameMaster", title = "Game master" } },
                characterStatuses = new[]
                {
                    new { name = "active", title = "Active" }, new { name = "retired", title = "Retired" }, new { name = "dead", title = "Dead" }
                },
                statTypes = new[] { new { name = "number", title = "Number" }, new { name = "text", title = "Text" } }
            }),
            Entities = matches.Skip((pageNumber - 1) * PageSize).Take(PageSize).Select(toItem).ToArray(),
            Links = links,
            Actions = actions
        };
    }

    private static string PageHref(string path, Dictionary<string, string> query, int page)
    {
        var parameters = query.Where(pair => pair.Key != "page").Select(pair => $"{pair.Key}={pair.Value}").ToList();
        if (page > 1)
        {
            parameters.Add($"page={page}");
        }

        return parameters.Count == 0 ? path : $"{path}?{string.Join('&', parameters)}";
    }

    private SirenEntity PlayerEntity(FakePlayer player, bool includeArchived)
    {
        var path = $"{PlayersHref}/{player.Id:D}";
        var actions = new List<SirenAction> { Action("update-player", path, "PUT") };
        if (player.IsActive)
        {
            actions.Add(Action("create-character", $"{path}/characters", "POST"));
            actions.Add(Action("deactivate-player", $"{path}/deactivate", "POST"));
        }
        else if (!player.IsArchived)
        {
            actions.Add(Action("activate-player", $"{path}/activate", "POST"));
        }

        actions.Add(player.IsArchived
            ? Action("unarchive-player", $"{path}/unarchive", "POST")
            : Action("archive-player", $"{path}/archive", "POST"));
        return new SirenEntity
        {
            Class = ["player"],
            Properties = PlayerProperties(player),
            Entities = Characters
                .Where(character => character.PlayerId == player.Id && (includeArchived || !character.IsArchived))
                .Select(CharacterItem)
                .ToArray(),
            Links =
            [
                new SirenLink { Rel = ["self"], Href = path },
                new SirenLink { Rel = [SirenRelations.WithArchived], Href = $"{path}?includeArchived=true" },
                new SirenLink { Rel = [SirenRelations.CharacterStats], Href = StatsHref },
                new SirenLink { Rel = [SirenRelations.Networks], Href = NetworksHref }
            ],
            Actions = CanWrite ? actions : null
        };
    }

    private SirenSubEntity CharacterItem(FakeCharacter character) =>
        new() { Rel = ["item"], Href = $"{CharactersHref}/{character.Id:D}", Properties = CharacterProperties(character) };

    private SirenEntity CharacterEntity(FakeCharacter character)
    {
        var path = $"{CharactersHref}/{character.Id:D}";
        var actions = new List<SirenAction>
        {
            Action("update-character", path, "PUT"),
            Action("regenerate-qr-identifier", $"{path}/regenerate-qr-identifier", "POST")
        };
        if (character.IsArchived)
        {
            actions.Add(Action("unarchive-character", $"{path}/unarchive", "POST"));
        }
        else if (character.Status != "active")
        {
            actions.Add(Action("archive-character", $"{path}/archive", "POST"));
        }

        return new SirenEntity
        {
            Class = ["character"],
            Properties = CharacterProperties(character),
            Links =
            [
                new SirenLink { Rel = ["self"], Href = path },
                new SirenLink { Rel = [SirenRelations.CharacterSkills], Href = $"{path}/skills" },
                new SirenLink { Rel = [SirenRelations.CharacterStats], Href = StatsHref },
                new SirenLink { Rel = [SirenRelations.Networks], Href = NetworksHref }
            ],
            Actions = CanWrite ? actions : null
        };
    }

    private SirenEntity CharacterSkillsEntity(FakeCharacter character)
    {
        var path = $"{CharactersHref}/{character.Id:D}/skills";
        var spent = character.Purchases.Sum(purchase => Skills.Single(skill => skill.Id == purchase.SkillId).Cost * purchase.Count);
        return new SirenEntity
        {
            Class = ["character-skills"],
            Properties = Json(new
            {
                characterId = character.Id,
                characterName = character.Name,
                experiencePoints = character.ExperiencePoints,
                freePoints = character.ExperiencePoints - spent,
                talentLimit = TalentLimit,
                talents = character.TalentIds.Select(id => Talents.Single(talent => talent.Id == id))
                    .Select(talent => new { talentId = talent.Id, name = talent.Name, isArchived = talent.IsArchived }),
                skills = character.Purchases.Select(purchase =>
                {
                    var skill = Skills.Single(item => item.Id == purchase.SkillId);
                    return new
                    {
                        skillId = skill.Id,
                        name = skill.Name,
                        talentId = purchase.TalentId,
                        count = purchase.Count,
                        specifics = purchase.Specifics,
                        cost = skill.Cost,
                        isMultiPurchase = skill.IsMultiPurchase,
                        isArchived = skill.IsArchived
                    };
                }),
                version = character.Version
            }),
            Links =
            [
                new SirenLink { Rel = ["self"], Href = path },
                new SirenLink { Rel = [SirenRelations.Talents], Href = TalentsHref },
                new SirenLink { Rel = [SirenRelations.Skills], Href = SkillsHref }
            ],
            Actions = CanWrite ? [Action("save-character-skills", path, "PUT")] : null
        };
    }

    private static SirenEntity CatalogCollection(string path, bool includeArchived, IEnumerable<JsonElement> items) =>
        new()
        {
            Class = ["collection"],
            Properties = Json(new { title = "Catalog", includesArchived = includeArchived }),
            Entities = items.Select(properties => new SirenSubEntity { Rel = ["item"], Properties = properties }).ToArray(),
            Links = [new SirenLink { Rel = [SirenRelations.WithArchived], Href = $"{path}?includeArchived=true" }]
        };

    private static JsonElement TalentProperties(FakeRosterTalent talent) =>
        Json(new { talentId = talent.Id, name = talent.Name, description = (string?)null, isArchived = talent.IsArchived, version = 1 });

    private static JsonElement SkillProperties(FakeRosterSkill skill) =>
        Json(new
        {
            skillId = skill.Id,
            name = skill.Name,
            description = (string?)null,
            cost = skill.Cost,
            isMultiPurchase = skill.IsMultiPurchase,
            isAdvanced = false,
            talentIds = new[] { skill.TalentId },
            gameFlags = Array.Empty<string>(),
            cardFlagIds = Array.Empty<Guid>(),
            prerequisiteGroups = Array.Empty<Guid[]>(),
            isArchived = skill.IsArchived,
            version = 1
        });

    private SirenEntity StatCollection(bool includeArchived) =>
        new()
        {
            Class = ["character-stat-collection"],
            Properties = Json(new { title = "Stats", description = "Stats", includesArchived = includeArchived }),
            Entities = Stats
                .Where(stat => includeArchived || !stat.IsArchived)
                .Select(stat => new SirenSubEntity { Rel = ["item"], Href = $"{StatsHref}/{stat.Id:D}", Properties = StatProperties(stat) })
                .ToArray(),
            Links = [new SirenLink { Rel = [SirenRelations.WithArchived], Href = $"{StatsHref}?includeArchived=true" }]
        };

    private SirenEntity StatEntity(FakeStat stat)
    {
        var path = $"{StatsHref}/{stat.Id:D}";
        return new SirenEntity
        {
            Class = ["character-stat"],
            Properties = StatProperties(stat),
            Links = [new SirenLink { Rel = ["self"], Href = path }],
            Actions = CanWrite
                ?
                [
                    Action("rename-character-stat", path, "PUT"),
                    stat.IsArchived
                        ? Action("unarchive-character-stat", $"{path}/unarchive", "POST")
                        : Action("archive-character-stat", $"{path}/archive", "POST")
                ]
                : null
        };
    }

    private SirenEntity NetworkCollection(bool includeDisabled) =>
        new()
        {
            Class = ["network-collection"],
            Properties = Json(new { title = "Networks", includesDisabled = includeDisabled }),
            Entities = Networks
                .Where(network => includeDisabled || network.IsEnabled)
                .Select(network => new SirenSubEntity
                {
                    Rel = ["item"],
                    Href = $"{NetworksHref}/{network.Id:D}",
                    Properties = Json(new
                    {
                        networkId = network.Id,
                        code = network.Name.ToLowerInvariant(),
                        name = network.Name,
                        theme = "blue",
                        isEnabled = network.IsEnabled,
                        isDefault = network.IsDefault,
                        version = 1
                    })
                })
                .ToArray(),
            Links = [new SirenLink { Rel = [SirenRelations.WithDisabled], Href = $"{NetworksHref}?includeDisabled=true" }]
        };

    private static JsonElement PlayerProperties(FakePlayer player) =>
        Json(new
        {
            playerId = player.Id,
            number = player.Number,
            displayName = player.Name,
            type = "player",
            status = player.IsActive ? "active" : "inactive",
            isArchived = player.IsArchived,
            version = player.Version
        });

    private JsonElement CharacterProperties(FakeCharacter character) =>
        Json(new
        {
            characterId = character.Id,
            number = character.Number,
            playerId = character.PlayerId,
            playerDisplayName = Players.Single(player => player.Id == character.PlayerId).Name,
            name = character.Name,
            experiencePoints = character.ExperiencePoints,
            status = character.Status,
            networkId = character.NetworkId,
            qrIdentifier = character.QrIdentifier,
            stats = Stats.Select(stat => new
            {
                statId = stat.Id,
                name = stat.Name,
                type = stat.Type,
                number = stat.Type == "number" ? character.Numbers.GetValueOrDefault(stat.Id) : (int?)null,
                text = stat.Type == "text" ? character.Texts.GetValueOrDefault(stat.Id, string.Empty) : null,
                isArchived = stat.IsArchived
            }).ToArray(),
            isArchived = character.IsArchived,
            version = character.Version
        });

    private static JsonElement StatProperties(FakeStat stat) =>
        Json(new { statId = stat.Id, name = stat.Name, type = stat.Type, defaultNumber = stat.DefaultNumber, defaultText = stat.DefaultText, isArchived = stat.IsArchived, version = stat.Version });

    private static void RequireVersion(JsonElement body, long version)
    {
        if (body.GetProperty("version").GetInt64() != version)
        {
            throw new ManagementApiException("errors.staleVersion", HttpStatusCode.Conflict);
        }
    }

    private FakePlayer FindPlayer(string id) =>
        Players.SingleOrDefault(player => player.Id == Guid.Parse(id)) ?? throw NotFound();

    private FakeCharacter FindCharacter(string id) =>
        Characters.SingleOrDefault(character => character.Id == Guid.Parse(id)) ?? throw NotFound();

    private FakeCharacter FindByQr(string? identifier) =>
        Guid.TryParse(identifier, out _)
            ? Characters.SingleOrDefault(character => character.QrIdentifier == identifier) ?? throw NotFound()
            : throw new ManagementApiException("errors.invalidQrIdentifier", HttpStatusCode.BadRequest);

    private FakeStat FindStat(string id) =>
        Stats.SingleOrDefault(stat => stat.Id == Guid.Parse(id)) ?? throw NotFound();

    private static Dictionary<string, string> Query(string query) =>
        query.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(pair => pair[0], pair => Uri.UnescapeDataString(pair.Length > 1 ? pair[1] : string.Empty));

    private static SirenAction Action(string name, string href, string method) =>
        new() { Name = name, Href = href, Method = method, Type = "application/json" };

    private static ManagementApiException NotFound() => new("errors.notFound", HttpStatusCode.NotFound);

    private static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value, JsonOptions);
}

internal sealed class FakePlayer(Guid id, int number, string name)
{
    public Guid Id { get; } = id;

    public int Number { get; } = number;

    public string Name { get; set; } = name;

    public bool IsActive { get; set; } = true;

    public bool IsArchived { get; set; }

    public long Version { get; set; } = 1;
}

internal sealed class FakeCharacter(Guid id, int number, Guid playerId, string name)
{
    public Guid Id { get; } = id;

    public int Number { get; } = number;

    public Guid PlayerId { get; } = playerId;

    public string Name { get; set; } = name;

    public string Status { get; set; } = "active";

    public int ExperiencePoints { get; set; }

    public Guid? NetworkId { get; set; }

    public string QrIdentifier { get; set; } = Guid.NewGuid().ToString("N");

    public Dictionary<Guid, int> Numbers { get; } = [];

    public Dictionary<Guid, string> Texts { get; } = [];

    public List<Guid> TalentIds { get; set; } = [];

    public List<FakePurchase> Purchases { get; set; } = [];

    public bool IsArchived { get; set; }

    public long Version { get; set; } = 1;
}

internal sealed class FakeStat(Guid id, string name, string type)
{
    public Guid Id { get; } = id;

    public string Name { get; set; } = name;

    public string Type { get; } = type;

    public int? DefaultNumber { get; set; }

    public string? DefaultText { get; set; }

    public bool IsArchived { get; set; }

    public long Version { get; set; } = 1;
}

internal sealed record FakeRosterNetwork(Guid Id, string Name, bool IsEnabled, bool IsDefault);

internal sealed record FakeRosterTalent(Guid Id, string Name, bool IsArchived);

internal sealed record FakeRosterSkill(Guid Id, string Name, int Cost, Guid TalentId, bool IsMultiPurchase, bool IsArchived);

internal sealed record FakePurchase(Guid SkillId, Guid TalentId, int Count, string? Specifics);
