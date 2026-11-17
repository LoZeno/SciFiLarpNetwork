using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Characters;
using SciFiLarpNetwork.Backend.Domain.Players;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.Players;

/// <summary>
/// Builds the Siren representations of players, characters, and character stats. Write actions are advertised only
/// when <paramref name="canWrite"/> is set and the action applies to the current state; the server still authorizes
/// and validates every request.
/// </summary>
internal sealed class RosterResources(IApplicationText text, string pathBase, bool canWrite)
{
    private const string JsonType = "application/json";

    public string PlayersPath => $"{pathBase}/api/v1/players";

    public string PlayerPath(Guid playerId) => $"{PlayersPath}/{playerId:D}";

    public string CharactersPath => $"{pathBase}/api/v1/characters";

    public string CharacterPath(Guid characterId) => $"{CharactersPath}/{characterId:D}";

    public string StatsPath => $"{pathBase}/api/v1/character-stats";

    public string StatPath(Guid statId) => $"{StatsPath}/{statId:D}";

    private string NetworksPath => $"{pathBase}/api/v1/networks";

    public static string EnumTitleKey<T>(string prefix, T value)
        where T : struct, Enum
    {
        var name = value.ToString();
        return $"{prefix}.{char.ToLowerInvariant(name[0])}{name[1..]}";
    }

    public SirenEntity<RosterCollectionProperties> PlayerCollection(RosterPage<PlayerSummary> page, RosterQuery query) =>
        new()
        {
            Class = ["player-collection"],
            Properties = CollectionProperties("api.players", "api.playersDescription", page, query),
            Entities = page.Items.Select(player => new SirenSubEntity
            {
                Rel = ["item"],
                Class = ["player"],
                Href = PlayerPath(player.PlayerId),
                Properties = SirenJson.ToElement(player)
            }).ToArray(),
            Links =
            [
                .. PageLinks(PlayersPath, page, query),
                new SirenLink { Rel = [SirenRelations.Characters], Href = CharactersPath },
                new SirenLink { Rel = [SirenRelations.CharacterStats], Href = StatsPath },
                new SirenLink { Rel = [SirenRelations.Networks], Href = NetworksPath }
            ],
            Actions =
            [
                SearchAction("search-players", PlayersPath, "api.searchPlayers", query),
                SearchAction("search-characters", CharactersPath, "api.searchCharacters", query with { Search = null }),
                new SirenAction
                {
                    Name = "find-character-by-qr",
                    Href = $"{CharactersPath}/by-qr",
                    Method = "GET",
                    Title = text.GetLocalizedText("api.findCharacterByQr"),
                    Fields = [Field("identifier", "text", "characters.qrIdentifier", (string?)null)]
                },
                .. canWrite
                    ?
                    [
                        new SirenAction
                        {
                            Name = "create-player",
                            Href = PlayersPath,
                            Method = "POST",
                            Type = JsonType,
                            Title = text.GetLocalizedText("api.createPlayer"),
                            Fields = PlayerFields(null)
                        }
                    ]
                    : Array.Empty<SirenAction>()
            ]
        };

    public SirenEntity<RosterCollectionProperties> CharacterCollection(RosterPage<CharacterSummary> page, RosterQuery query) =>
        new()
        {
            Class = ["character-collection"],
            Properties = CollectionProperties("api.characters", "api.charactersDescription", page, query),
            Entities = page.Items.Select(CharacterItem).ToArray(),
            Links = [.. PageLinks(CharactersPath, page, query), new SirenLink { Rel = [SirenRelations.Players], Href = PlayersPath }],
            Actions = [SearchAction("search-characters", CharactersPath, "api.searchCharacters", query)]
        };

    public SirenEntity<PlayerSummary> Player(PlayerSummary player, IReadOnlyList<CharacterSummary> characters,
        bool includeArchivedCharacters)
    {
        var path = PlayerPath(player.PlayerId);
        return new SirenEntity<PlayerSummary>
        {
            Class = ["player"],
            Properties = player,
            Entities = characters.Select(CharacterItem).ToArray(),
            Links =
            [
                new SirenLink { Rel = ["self"], Href = includeArchivedCharacters ? WithArchived(path) : path },
                new SirenLink { Rel = ["collection"], Href = PlayersPath },
                new SirenLink { Rel = [SirenRelations.WithArchived], Href = WithArchived(path) },
                new SirenLink { Rel = [SirenRelations.WithoutArchived], Href = path },
                new SirenLink { Rel = [SirenRelations.CharacterStats], Href = StatsPath },
                new SirenLink { Rel = [SirenRelations.Networks], Href = NetworksPath }
            ],
            Actions = canWrite ? PlayerActions(player) : null
        };
    }

    public SirenEntity<CharacterSummary> Character(CharacterSummary character) =>
        new()
        {
            Class = ["character"],
            Properties = character,
            Links =
            [
                new SirenLink { Rel = ["self"], Href = CharacterPath(character.CharacterId) },
                new SirenLink { Rel = ["collection"], Href = CharactersPath },
                new SirenLink { Rel = [SirenRelations.Player], Href = PlayerPath(character.PlayerId) },
                new SirenLink { Rel = [SirenRelations.CharacterStats], Href = StatsPath },
                new SirenLink { Rel = [SirenRelations.Networks], Href = NetworksPath }
            ],
            Actions = canWrite ? CharacterActions(character) : null
        };

    public SirenEntity<CharacterStatCollectionProperties> StatCollection(IReadOnlyList<CharacterStatSummary> stats,
        bool includeArchived) =>
        new()
        {
            Class = ["character-stat-collection"],
            Properties = new CharacterStatCollectionProperties(
                text.GetLocalizedText("api.characterStats"),
                text.GetLocalizedText("api.characterStatsDescription"),
                includeArchived),
            Entities = stats.Select(stat => new SirenSubEntity
            {
                Rel = ["item"],
                Class = ["character-stat"],
                Href = StatPath(stat.StatId),
                Properties = SirenJson.ToElement(stat)
            }).ToArray(),
            Links =
            [
                new SirenLink { Rel = ["self"], Href = includeArchived ? WithArchived(StatsPath) : StatsPath },
                new SirenLink { Rel = [SirenRelations.WithArchived], Href = WithArchived(StatsPath) },
                new SirenLink { Rel = [SirenRelations.WithoutArchived], Href = StatsPath }
            ]
        };

    public SirenEntity<CharacterStatSummary> Stat(CharacterStatSummary stat)
    {
        var path = StatPath(stat.StatId);
        return new SirenEntity<CharacterStatSummary>
        {
            Class = ["character-stat"],
            Properties = stat,
            Links =
            [
                new SirenLink { Rel = ["self"], Href = path },
                new SirenLink { Rel = ["collection"], Href = StatsPath }
            ],
            Actions = canWrite
                ?
                [
                    new SirenAction
                    {
                        Name = "rename-character-stat",
                        Href = path,
                        Method = "PUT",
                        Type = JsonType,
                        Title = text.GetLocalizedText("api.renameCharacterStat"),
                        Fields = [VersionField(stat.Version), Field("name", "text", "stats.name", stat.Name)]
                    },
                    stat.IsArchived
                        ? SimpleAction("unarchive-character-stat", $"{path}/unarchive", "api.unarchiveCharacterStat")
                        : SimpleAction("archive-character-stat", $"{path}/archive", "api.archiveCharacterStat")
                ]
                : null
        };
    }

    private RosterCollectionProperties CollectionProperties<T>(
        string titleKey,
        string descriptionKey,
        RosterPage<T> page,
        RosterQuery query) =>
        new(
            text.GetLocalizedText(titleKey),
            text.GetLocalizedText(descriptionKey),
            string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            query.IncludeArchived,
            page.PageNumber,
            page.PageCount,
            page.TotalCount,
            Options<PlayerType>("players.type"),
            Options<CharacterStatus>("characters.status"),
            Options<CharacterStatType>("stats.type"));

    private RosterOption[] Options<T>(string prefix)
        where T : struct, Enum =>
        Enum.GetValues<T>()
            .Select(value => new RosterOption(
                SirenJson.ToElement(value).GetString()!,
                text.GetLocalizedText(EnumTitleKey(prefix, value))))
            .ToArray();

    private static SirenLink[] PageLinks<T>(string path, RosterPage<T> page, RosterQuery query)
    {
        var links = new List<SirenLink>
        {
            new() { Rel = ["self"], Href = PageHref(path, query) },
            new() { Rel = [SirenRelations.First], Href = PageHref(path, query with { PageNumber = 1 }) },
            new() { Rel = [SirenRelations.Last], Href = PageHref(path, query with { PageNumber = page.PageCount }) },
            new() { Rel = [SirenRelations.WithArchived], Href = PageHref(path, query with { IncludeArchived = true, PageNumber = 1 }) },
            new() { Rel = [SirenRelations.WithoutArchived], Href = PageHref(path, query with { IncludeArchived = false, PageNumber = 1 }) }
        };
        if (page.PageNumber > 1)
        {
            links.Add(new SirenLink
            {
                Rel = [SirenRelations.Previous],
                Href = PageHref(path, query with { PageNumber = Math.Min(page.PageNumber - 1, page.PageCount) })
            });
        }

        if (page.PageNumber < page.PageCount)
        {
            links.Add(new SirenLink { Rel = [SirenRelations.Next], Href = PageHref(path, query with { PageNumber = page.PageNumber + 1 }) });
        }

        return [.. links];
    }

    private static string PageHref(string path, RosterQuery query)
    {
        var parameters = new List<string>();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            parameters.Add($"search={Uri.EscapeDataString(query.Search.Trim())}");
        }

        if (query.IncludeArchived)
        {
            parameters.Add("includeArchived=true");
        }

        if (query.PageNumber > 1)
        {
            parameters.Add($"page={query.PageNumber}");
        }

        return parameters.Count == 0 ? path : $"{path}?{string.Join('&', parameters)}";
    }

    private static string WithArchived(string path) => $"{path}?includeArchived=true";

    private SirenAction SearchAction(string name, string href, string titleKey, RosterQuery query) =>
        new()
        {
            Name = name,
            Href = href,
            Method = "GET",
            Title = text.GetLocalizedText(titleKey),
            Fields =
            [
                Field("search", "search", "roster.search", string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim()),
                Field("includeArchived", "checkbox", "roster.includeArchived", query.IncludeArchived)
            ]
        };

    private SirenSubEntity CharacterItem(CharacterSummary character) =>
        new()
        {
            Rel = ["item"],
            Class = ["character"],
            Href = CharacterPath(character.CharacterId),
            Properties = SirenJson.ToElement(character)
        };

    private SirenAction[] PlayerActions(PlayerSummary player)
    {
        var path = PlayerPath(player.PlayerId);
        var actions = new List<SirenAction>
        {
            new()
            {
                Name = "update-player",
                Href = path,
                Method = "PUT",
                Type = JsonType,
                Title = text.GetLocalizedText("api.updatePlayer"),
                Fields = [VersionField(player.Version), .. PlayerFields(player)]
            }
        };

        if (player.Status == PlayerStatus.Active)
        {
            actions.Add(new SirenAction
            {
                Name = "create-character",
                Href = $"{path}/characters",
                Method = "POST",
                Type = JsonType,
                Title = text.GetLocalizedText("api.createCharacter"),
                Fields = CharacterFields(null)
            });
            actions.Add(SimpleAction("deactivate-player", $"{path}/deactivate", "api.deactivatePlayer"));
        }
        else if (!player.IsArchived)
        {
            actions.Add(SimpleAction("activate-player", $"{path}/activate", "api.activatePlayer"));
        }

        if (player.IsArchived)
        {
            actions.Add(SimpleAction("unarchive-player", $"{path}/unarchive", "api.unarchivePlayer"));
        }
        else if (player.Status == PlayerStatus.Inactive)
        {
            actions.Add(SimpleAction("archive-player", $"{path}/archive", "api.archivePlayer"));
        }

        return [.. actions];
    }

    private SirenAction[] CharacterActions(CharacterSummary character)
    {
        var path = CharacterPath(character.CharacterId);
        var actions = new List<SirenAction>
        {
            new()
            {
                Name = "update-character",
                Href = path,
                Method = "PUT",
                Type = JsonType,
                Title = text.GetLocalizedText("api.updateCharacter"),
                Fields =
                [
                    VersionField(character.Version),
                    .. CharacterFields(character),
                    Field("status", "radio", "characters.status", character.Status),
                    Field("deathDate", "date", "characters.deathDate", character.DeathDate)
                ]
            },
            SimpleAction("regenerate-qr-identifier", $"{path}/regenerate-qr-identifier", "api.regenerateQrIdentifier")
        };

        if (character.IsArchived)
        {
            actions.Add(SimpleAction("unarchive-character", $"{path}/unarchive", "api.unarchiveCharacter"));
        }
        else if (character.Status != CharacterStatus.Active)
        {
            actions.Add(SimpleAction("archive-character", $"{path}/archive", "api.archiveCharacter"));
        }

        return [.. actions];
    }

    private SirenField[] PlayerFields(PlayerSummary? player) =>
    [
        Field("displayName", "text", "players.displayName", player?.DisplayName),
        Field("email", "email", "players.email", player?.Email),
        Field("phone", "tel", "players.phone", player?.Phone),
        Field("notes", "textarea", "players.notes", player?.Notes),
        Field("type", "radio", "players.type", player?.Type)
    ];

    private SirenField[] CharacterFields(CharacterSummary? character) =>
    [
        Field("name", "text", "characters.name", character?.Name),
        Field("title", "text", "characters.title", character?.Title),
        Field("species", "text", "characters.species", character?.Species),
        Field("gender", "text", "characters.gender", character?.Gender),
        Field("type", "text", "characters.type", character?.Type),
        Field("experiencePoints", "number", "characters.experiencePoints", character?.ExperiencePoints ?? 0),
        Field("networkId", "text", "characters.network", character?.NetworkId),
        Field("statValues", "hidden", "characters.stats", (string?)null),
        Field("newStats", "hidden", "characters.newStats", (string?)null)
    ];

    private SirenAction SimpleAction(string name, string href, string titleKey) =>
        new() { Name = name, Href = href, Method = "POST", Type = JsonType, Title = text.GetLocalizedText(titleKey) };

    private SirenField VersionField(long version) =>
        new() { Name = "version", Type = "hidden", Value = SirenJson.ToElement(version), Title = text.GetLocalizedText("skills.version") };

    private SirenField Field<T>(string name, string type, string titleKey, T? value) =>
        new()
        {
            Name = name,
            Type = type,
            Title = text.GetLocalizedText(titleKey),
            Value = value is null ? null : SirenJson.ToElement(value)
        };
}
