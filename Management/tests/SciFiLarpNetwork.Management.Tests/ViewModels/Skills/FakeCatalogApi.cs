using System.Net;
using System.Text.Json;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Skills;

/// <summary>
/// An in-memory talent and skill catalog that answers with the same Siren shape, links and actions as the backend.
/// </summary>
internal sealed class FakeCatalogApi : IManagementApiClient
{
    public const string TalentsHref = "/api/v1/talents";
    private const string SkillsHref = "/api/v1/skills";
    private const string CardFlagsHref = "/api/v1/skill-card-flags";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public bool CanWrite { get; init; } = true;

    public List<FakeTalent> Talents { get; } = [];

    public List<FakeSkill> Skills { get; } = [];

    public List<FakeCardFlag> CardFlags { get; } = [];

    public List<Submission> Submissions { get; } = [];

    /// <summary>One-shot failures for the next submission of the named action.</summary>
    public Dictionary<string, Exception> SubmitFailures { get; } = [];

    public Exception? GetFailure { get; set; }

    public FakeTalent AddTalent(string name, bool isArchived = false)
    {
        var talent = new FakeTalent(Guid.NewGuid(), name) { IsArchived = isArchived };
        Talents.Add(talent);
        return talent;
    }

    public FakeSkill AddSkill(string name, params FakeTalent[] talents)
    {
        var skill = new FakeSkill(Guid.NewGuid(), name);
        skill.TalentIds.AddRange(talents.Select(talent => talent.Id));
        Skills.Add(skill);
        return skill;
    }

    public Task<SirenEntity> GetResourceAsync(
        string apiBaseUrl,
        string href,
        string locale,
        CancellationToken cancellationToken = default)
    {
        if (GetFailure is { } failure)
        {
            return Task.FromException<SirenEntity>(failure);
        }

        var parts = href.Split('?');
        var includeArchived = parts.Length > 1 && parts[1] == "includeArchived=true";
        var segments = parts[0].Split('/', StringSplitOptions.RemoveEmptyEntries);
        return Task.FromResult(segments switch
        {
            ["api", "v1", "talents"] => TalentCollection(includeArchived),
            ["api", "v1", "talents", var id] => TalentEntity(FindTalent(id)),
            ["api", "v1", "talents", var id, "skills"] => SkillCollection(
                $"{TalentsHref}/{id}/skills",
                FindTalent(id).Id,
                includeArchived),
            ["api", "v1", "skills"] => SkillCollection(SkillsHref, null, includeArchived),
            ["api", "v1", "skills", var id] => SkillEntity(FindSkill(id)),
            ["api", "v1", "skill-card-flags"] => CardFlagCollection(),
            _ => throw new ManagementApiException("errors.notFound", HttpStatusCode.NotFound)
        });
    }

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

        return Task.FromResult(Apply(action, body));
    }

    public Task<ManagementSession> SignInAsync(
        string apiBaseUrl,
        string userName,
        string password,
        string locale,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ManagementSession(userName, "administrator", DateTimeOffset.UtcNow.AddHours(1)));

    public Task SignOutAsync(string apiBaseUrl, string locale, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<ManagementSession> RenewSessionAsync(
        string apiBaseUrl,
        string locale,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ManagementSession("operator", "administrator", DateTimeOffset.UtcNow.AddHours(1)));

    public Task<ApiDiscovery> DiscoverAsync(string apiBaseUrl, string locale, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ApiDiscovery(
            new SirenEntity { Class = ["api"], Properties = JsonSerializer.SerializeToElement(new { title = "Administration" }) },
            [new SirenLink { Rel = [SirenRelations.Navigation, SirenRelations.Talents], Href = TalentsHref, Title = "Talents" }]));

    private SirenEntity Apply(SirenAction action, JsonElement? body)
    {
        var segments = action.Href.Split('/', StringSplitOptions.RemoveEmptyEntries);
        switch (action.Name)
        {
            case "create-talent":
                var created = new FakeTalent(Guid.NewGuid(), body!.Value.GetProperty("name").GetString()!)
                {
                    Description = OptionalString(body.Value, "description")
                };
                Talents.Add(created);
                return TalentEntity(created);
            case "update-talent":
                var talent = FindTalent(segments[3]);
                RequireVersion(body!.Value, talent.Version);
                talent.Name = body.Value.GetProperty("name").GetString()!;
                talent.Description = OptionalString(body.Value, "description");
                talent.Version++;
                return TalentEntity(talent);
            case "archive-talent" or "unarchive-talent":
                var archivedTalent = FindTalent(segments[3]);
                archivedTalent.IsArchived = action.Name == "archive-talent";
                archivedTalent.Version++;
                return TalentEntity(archivedTalent);
            case "create-skill":
                var skill = new FakeSkill(Guid.NewGuid(), string.Empty);
                ApplySkill(skill, body!.Value);
                Skills.Add(skill);
                return SkillEntity(skill);
            case "update-skill":
                var edited = FindSkill(segments[3]);
                RequireVersion(body!.Value, edited.Version);
                ApplySkill(edited, body.Value);
                edited.Version++;
                return SkillEntity(edited);
            case "archive-skill" or "unarchive-skill":
                var archivedSkill = FindSkill(segments[3]);
                archivedSkill.IsArchived = action.Name == "archive-skill";
                archivedSkill.Version++;
                return SkillEntity(archivedSkill);
            default:
                throw new ManagementApiException("errors.notFound", HttpStatusCode.NotFound);
        }
    }

    private static void RequireVersion(JsonElement body, long current)
    {
        if (body.GetProperty("version").GetInt64() != current)
        {
            throw new ManagementApiException("errors.staleVersion", HttpStatusCode.Conflict);
        }
    }

    private static string? OptionalString(JsonElement body, string name) =>
        body.TryGetProperty(name, out var value) ? value.GetString() : null;

    private static void ApplySkill(FakeSkill skill, JsonElement body)
    {
        skill.Name = body.GetProperty("name").GetString()!;
        skill.Description = OptionalString(body, "description");
        skill.Cost = body.GetProperty("cost").GetInt32();
        skill.TalentIds.Clear();
        skill.TalentIds.AddRange(body.GetProperty("talentIds").EnumerateArray().Select(id => id.GetGuid()));
        skill.GameFlags.Clear();
        skill.GameFlags.AddRange(body.GetProperty("gameFlags").EnumerateArray().Select(flag => flag.GetString()!));
        skill.PrerequisiteGroups.Clear();
        skill.PrerequisiteGroups.AddRange(body.GetProperty("prerequisiteGroups").EnumerateArray()
            .Select(group => group.EnumerateArray().Select(id => id.GetGuid()).ToArray()));
    }

    private FakeTalent FindTalent(string id) =>
        Talents.SingleOrDefault(talent => talent.Id == Guid.Parse(id))
        ?? throw new ManagementApiException("errors.notFound", HttpStatusCode.NotFound);

    private FakeSkill FindSkill(string id) =>
        Skills.SingleOrDefault(skill => skill.Id == Guid.Parse(id))
        ?? throw new ManagementApiException("errors.notFound", HttpStatusCode.NotFound);

    private SirenEntity TalentCollection(bool includeArchived) =>
        new()
        {
            Class = ["talent-collection"],
            Properties = Json(new { title = "Talents", includesArchived = includeArchived }),
            Entities = Talents
                .Where(talent => includeArchived || !talent.IsArchived)
                .Select(talent => new SirenSubEntity
                {
                    Rel = ["item"],
                    Href = TalentHref(talent),
                    Properties = TalentProperties(talent),
                    Links = [new SirenLink { Rel = [SirenRelations.Skills], Href = $"{TalentHref(talent)}/skills" }]
                })
                .ToArray(),
            Links =
            [
                .. FilterLinks(TalentsHref),
                new SirenLink { Rel = [SirenRelations.Skills], Href = SkillsHref },
                new SirenLink { Rel = [SirenRelations.CardFlags], Href = CardFlagsHref }
            ],
            Actions = CanWrite ? [Action("create-talent", TalentsHref, "POST")] : null
        };

    private SirenEntity TalentEntity(FakeTalent talent) =>
        new()
        {
            Class = ["talent"],
            Properties = TalentProperties(talent),
            Actions = CanWrite
                ?
                [
                    Action("update-talent", TalentHref(talent), "PUT"),
                    talent.IsArchived
                        ? Action("unarchive-talent", $"{TalentHref(talent)}/unarchive", "POST")
                        : Action("archive-talent", $"{TalentHref(talent)}/archive", "POST")
                ]
                : null
        };

    private SirenEntity SkillCollection(string path, Guid? talentId, bool includeArchived) =>
        new()
        {
            Class = ["skill-collection"],
            Properties = Json(new
            {
                title = "Skills",
                includesArchived = includeArchived,
                gameFlags = new[]
                {
                    new { name = "accessHacking", title = "Access Hacking" },
                    new { name = "improvesHacking", title = "Improves Hacking" }
                }
            }),
            Entities = Skills
                .Where(skill => talentId is not { } id || skill.TalentIds.Contains(id))
                .Where(skill => includeArchived || !skill.IsArchived)
                .Select(skill => new SirenSubEntity { Rel = ["item"], Href = SkillHref(skill), Properties = SkillProperties(skill) })
                .ToArray(),
            Links =
            [
                .. FilterLinks(path),
                new SirenLink { Rel = [SirenRelations.Talents], Href = TalentsHref },
                new SirenLink { Rel = [SirenRelations.CardFlags], Href = CardFlagsHref }
            ],
            Actions = CanWrite ? [Action("create-skill", SkillsHref, "POST")] : null
        };

    private SirenEntity SkillEntity(FakeSkill skill) =>
        new()
        {
            Class = ["skill"],
            Properties = SkillProperties(skill),
            Actions = CanWrite
                ?
                [
                    Action("update-skill", SkillHref(skill), "PUT"),
                    skill.IsArchived
                        ? Action("unarchive-skill", $"{SkillHref(skill)}/unarchive", "POST")
                        : Action("archive-skill", $"{SkillHref(skill)}/archive", "POST")
                ]
                : null
        };

    private SirenEntity CardFlagCollection() =>
        new()
        {
            Class = ["card-flag-collection"],
            Properties = Json(new
            {
                title = "Card flags",
                cardFlags = CardFlags.Select(flag => new { cardFlagId = flag.Id, name = flag.Name }).ToArray()
            })
        };

    private static SirenLink[] FilterLinks(string path) =>
    [
        new SirenLink { Rel = [SirenRelations.WithArchived], Href = $"{path}?includeArchived=true" },
        new SirenLink { Rel = [SirenRelations.WithoutArchived], Href = path }
    ];

    private static SirenAction Action(string name, string href, string method) =>
        new() { Name = name, Href = href, Method = method, Type = "application/json" };

    private static string TalentHref(FakeTalent talent) => $"{TalentsHref}/{talent.Id:D}";

    private static string SkillHref(FakeSkill skill) => $"{SkillsHref}/{skill.Id:D}";

    private static JsonElement TalentProperties(FakeTalent talent) =>
        Json(new
        {
            talentId = talent.Id,
            name = talent.Name,
            description = talent.Description,
            isArchived = talent.IsArchived,
            version = talent.Version
        });

    private static JsonElement SkillProperties(FakeSkill skill) =>
        Json(new
        {
            skillId = skill.Id,
            name = skill.Name,
            description = skill.Description,
            cost = skill.Cost,
            isMultiPurchase = skill.IsMultiPurchase,
            isAdvanced = skill.IsAdvanced,
            talentIds = skill.TalentIds,
            gameFlags = skill.GameFlags,
            cardFlagIds = skill.CardFlagIds,
            prerequisiteGroups = skill.PrerequisiteGroups,
            isArchived = skill.IsArchived,
            version = skill.Version
        });

    private static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value, JsonOptions);
}

internal sealed record Submission(string ActionName, string? Body);

internal sealed record FakeCardFlag(Guid Id, string Name);

internal sealed class FakeTalent(Guid id, string name)
{
    public Guid Id { get; } = id;

    public string Name { get; set; } = name;

    public string? Description { get; set; }

    public bool IsArchived { get; set; }

    public long Version { get; set; } = 1;
}

internal sealed class FakeSkill(Guid id, string name)
{
    public Guid Id { get; } = id;

    public string Name { get; set; } = name;

    public string? Description { get; set; }

    public int Cost { get; set; }

    public bool IsMultiPurchase { get; set; }

    public bool IsAdvanced { get; set; }

    public List<Guid> TalentIds { get; } = [];

    public List<string> GameFlags { get; } = [];

    public List<Guid> CardFlagIds { get; } = [];

    public List<Guid[]> PrerequisiteGroups { get; } = [];

    public bool IsArchived { get; set; }

    public long Version { get; set; } = 1;
}
