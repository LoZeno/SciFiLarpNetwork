using SciFiLarpNetwork.Backend.Application.Skills;
using SciFiLarpNetwork.Backend.Domain.Skills;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.Skills;

/// <summary>
/// Builds the Siren representations of the talent and skill catalog. Write actions are advertised only when
/// <paramref name="canWrite"/> is set; the server still authorizes every request.
/// </summary>
internal sealed class SkillCatalogResources(IApplicationText text, string pathBase, bool canWrite)
{
    private const string JsonType = "application/json";

    public string TalentsPath => $"{pathBase}/api/v1/talents";

    public string SkillsPath => $"{pathBase}/api/v1/skills";

    public string CardFlagsPath => $"{pathBase}/api/v1/skill-card-flags";

    public string TalentPath(Guid talentId) => $"{TalentsPath}/{talentId:D}";

    public string TalentSkillsPath(Guid talentId) => $"{TalentPath(talentId)}/skills";

    public string SkillPath(Guid skillId) => $"{SkillsPath}/{skillId:D}";

    public SirenEntity<CatalogCollectionProperties> TalentCollection(IReadOnlyList<TalentSummary> talents, bool includeArchived) =>
        new()
        {
            Class = ["talent-collection"],
            Properties = new CatalogCollectionProperties(
                text.GetLocalizedText("api.talents"),
                text.GetLocalizedText("api.talentsDescription"),
                includeArchived),
            Entities = talents.Select(talent => new SirenSubEntity
            {
                Rel = ["item"],
                Class = ["talent"],
                Href = TalentPath(talent.TalentId),
                Properties = SirenJson.ToElement(talent)
            }).ToArray(),
            Links =
            [
                new SirenLink { Rel = ["self"], Href = WithArchived(TalentsPath, includeArchived) },
                new SirenLink { Rel = [SirenRelations.Skills], Href = SkillsPath, Title = text.GetLocalizedText("api.skills") },
                new SirenLink { Rel = [SirenRelations.CardFlags], Href = CardFlagsPath, Title = text.GetLocalizedText("api.cardFlags") }
            ],
            Actions = canWrite
                ?
                [
                    new SirenAction
                    {
                        Name = "create-talent",
                        Href = TalentsPath,
                        Method = "POST",
                        Type = JsonType,
                        Title = text.GetLocalizedText("api.createTalent"),
                        Fields = TalentFields(null)
                    }
                ]
                : null
        };

    public SirenEntity<TalentSummary> Talent(TalentSummary talent) =>
        new()
        {
            Class = ["talent"],
            Properties = talent,
            Links =
            [
                new SirenLink { Rel = ["self"], Href = TalentPath(talent.TalentId) },
                new SirenLink { Rel = ["collection"], Href = TalentsPath },
                new SirenLink { Rel = [SirenRelations.Skills], Href = TalentSkillsPath(talent.TalentId), Title = text.GetLocalizedText("api.skills") }
            ],
            Actions = canWrite
                ?
                [
                    new SirenAction
                    {
                        Name = "update-talent",
                        Href = TalentPath(talent.TalentId),
                        Method = "PUT",
                        Type = JsonType,
                        Title = text.GetLocalizedText("api.updateTalent"),
                        Fields = [VersionField(talent.Version), .. TalentFields(talent)]
                    },
                    ArchiveAction("talent", TalentPath(talent.TalentId), talent.IsArchived)
                ]
                : null
        };

    public SirenEntity<SkillCollectionProperties> SkillCollection(
        IReadOnlyList<SkillSummary> skills,
        bool includeArchived,
        string collectionPath) =>
        new()
        {
            Class = ["skill-collection"],
            Properties = new SkillCollectionProperties(
                text.GetLocalizedText("api.skills"),
                text.GetLocalizedText("api.skillsDescription"),
                includeArchived,
                Enum.GetValues<GameFlag>()
                    .Select(flag => new GameFlagOption(flag, text.GetLocalizedText(GameFlagTitleKey(flag))))
                    .ToArray()),
            Entities = skills.Select(skill => new SirenSubEntity
            {
                Rel = ["item"],
                Class = ["skill"],
                Href = SkillPath(skill.SkillId),
                Properties = SirenJson.ToElement(skill)
            }).ToArray(),
            Links =
            [
                new SirenLink { Rel = ["self"], Href = WithArchived(collectionPath, includeArchived) },
                new SirenLink { Rel = [SirenRelations.Talents], Href = TalentsPath, Title = text.GetLocalizedText("api.talents") },
                new SirenLink { Rel = [SirenRelations.CardFlags], Href = CardFlagsPath, Title = text.GetLocalizedText("api.cardFlags") }
            ],
            Actions = canWrite
                ?
                [
                    new SirenAction
                    {
                        Name = "create-skill",
                        Href = SkillsPath,
                        Method = "POST",
                        Type = JsonType,
                        Title = text.GetLocalizedText("api.createSkill"),
                        Fields = SkillFields(null)
                    }
                ]
                : null
        };

    public SirenEntity<SkillSummary> Skill(SkillSummary skill) =>
        new()
        {
            Class = ["skill"],
            Properties = skill,
            Links =
            [
                new SirenLink { Rel = ["self"], Href = SkillPath(skill.SkillId) },
                new SirenLink { Rel = ["collection"], Href = SkillsPath }
            ],
            Actions = canWrite
                ?
                [
                    new SirenAction
                    {
                        Name = "update-skill",
                        Href = SkillPath(skill.SkillId),
                        Method = "PUT",
                        Type = JsonType,
                        Title = text.GetLocalizedText("api.updateSkill"),
                        Fields = [VersionField(skill.Version), .. SkillFields(skill)]
                    },
                    ArchiveAction("skill", SkillPath(skill.SkillId), skill.IsArchived)
                ]
                : null
        };

    public SirenEntity<CardFlagCollectionProperties> CardFlagCollection(IReadOnlyList<CardFlagSummary> cardFlags) =>
        new()
        {
            Class = ["card-flag-collection"],
            Properties = new CardFlagCollectionProperties(text.GetLocalizedText("api.cardFlags"), cardFlags),
            Links =
            [
                new SirenLink { Rel = ["self"], Href = CardFlagsPath },
                new SirenLink { Rel = [SirenRelations.Skills], Href = SkillsPath, Title = text.GetLocalizedText("api.skills") }
            ]
        };

    public static string GameFlagTitleKey(GameFlag flag)
    {
        var name = flag.ToString();
        return $"skills.gameFlag.{char.ToLowerInvariant(name[0])}{name[1..]}";
    }

    private static string WithArchived(string path, bool includeArchived) =>
        includeArchived ? $"{path}?includeArchived=true" : path;

    private SirenAction ArchiveAction(string kind, string resourcePath, bool isArchived) =>
        new()
        {
            Name = isArchived ? $"unarchive-{kind}" : $"archive-{kind}",
            Href = isArchived ? $"{resourcePath}/unarchive" : $"{resourcePath}/archive",
            Method = "POST",
            Type = JsonType,
            Title = text.GetLocalizedText(isArchived ? $"api.unarchive{Capitalize(kind)}" : $"api.archive{Capitalize(kind)}")
        };

    private static string Capitalize(string value) => char.ToUpperInvariant(value[0]) + value[1..];

    private SirenField VersionField(long version) =>
        new() { Name = "version", Type = "hidden", Value = SirenJson.ToElement(version), Title = text.GetLocalizedText("skills.version") };

    private SirenField[] TalentFields(TalentSummary? talent) =>
    [
        Field("name", "text", "skills.name", talent?.Name),
        Field("description", "textarea", "skills.description", talent?.Description)
    ];

    private SirenField[] SkillFields(SkillSummary? skill) =>
    [
        Field("name", "text", "skills.name", skill?.Name),
        Field("description", "textarea", "skills.description", skill?.Description),
        Field("cost", "number", "skills.cost", skill?.Cost),
        Field("isMultiPurchase", "checkbox", "skills.multiPurchase", skill?.IsMultiPurchase),
        Field("isAdvanced", "checkbox", "skills.advanced", skill?.IsAdvanced),
        Field("talentIds", "list", "skills.talents", skill?.TalentIds),
        Field("gameFlags", "list", "skills.gameFlags", skill?.GameFlags),
        Field("cardFlagIds", "list", "skills.cardFlags", skill?.CardFlagIds),
        Field("newCardFlagNames", "list", "skills.newCardFlags", (string[]?)null),
        Field("prerequisiteGroups", "list", "skills.prerequisites", skill?.PrerequisiteGroups)
    ];

    private SirenField Field<T>(string name, string type, string titleKey, T? value) =>
        new()
        {
            Name = name,
            Type = type,
            Title = text.GetLocalizedText(titleKey),
            Value = value is null ? null : SirenJson.ToElement(value)
        };
}
