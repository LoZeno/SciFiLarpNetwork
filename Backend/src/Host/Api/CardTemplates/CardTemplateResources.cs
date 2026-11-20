using SciFiLarpNetwork.Backend.Application.CardTemplates;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.CardTemplates;

/// <summary>
/// Builds the Siren representations of card templates. Readers get the source link; upload, preview, and reset are
/// advertised only when <paramref name="canWrite"/> is set, and reset only while an uploaded template is in use.
/// The server still authorizes and validates every request.
/// </summary>
internal sealed class CardTemplateResources(IApplicationText text, string pathBase, bool canWrite)
{
    public const string CharacterKind = "character";
    private const string JsonType = "application/json";

    public string CollectionPath => $"{pathBase}/api/v1/card-templates";

    public string CharacterPath => $"{CollectionPath}/{CharacterKind}";

    public SirenEntity<CardTemplateCollectionProperties> Collection(CardTemplateSummary character) =>
        new()
        {
            Class = ["card-template-collection"],
            Properties = new CardTemplateCollectionProperties(
                text.GetLocalizedText("api.cardTemplates"),
                text.GetLocalizedText("api.cardTemplatesDescription")),
            Entities =
            [
                new SirenSubEntity
                {
                    Rel = ["item"],
                    Class = ["card-template"],
                    Href = CharacterPath,
                    Properties = SirenJson.ToElement(Properties(character))
                }
            ],
            Links = [new SirenLink { Rel = ["self"], Href = CollectionPath }]
        };

    public SirenEntity<CardTemplateProperties> Character(CardTemplateSummary character) =>
        new()
        {
            Class = ["card-template"],
            Properties = Properties(character),
            Links =
            [
                new SirenLink { Rel = ["self"], Href = CharacterPath },
                new SirenLink { Rel = ["collection"], Href = CollectionPath },
                new SirenLink
                {
                    Rel = [SirenRelations.TemplateSource],
                    Href = $"{CharacterPath}/source",
                    Type = CardTemplatesController.TemplateMediaType,
                    Title = text.GetLocalizedText("api.downloadCardTemplate")
                }
            ],
            Actions = canWrite ? Actions(character) : null
        };

    private CardTemplateProperties Properties(CardTemplateSummary summary) =>
        new(CharacterKind, text.GetLocalizedText("cardTemplates.character"), summary.IsDefault, summary.UploadedBy,
            summary.UploadedAtUtc, summary.Version);

    private SirenAction[] Actions(CardTemplateSummary character)
    {
        var actions = new List<SirenAction>
        {
            new()
            {
                Name = "upload-card-template",
                Href = CharacterPath,
                Method = "PUT",
                Type = JsonType,
                Title = text.GetLocalizedText("api.uploadCardTemplate"),
                Fields = [VersionField(character.Version), ContentField()]
            },
            new()
            {
                Name = "preview-card-template",
                Href = $"{CharacterPath}/preview",
                Method = "POST",
                Type = JsonType,
                Title = text.GetLocalizedText("api.previewCardTemplate"),
                Fields = [ContentField()]
            }
        };

        if (!character.IsDefault)
        {
            actions.Add(new SirenAction
            {
                Name = "reset-card-template",
                Href = $"{CharacterPath}/reset",
                Method = "POST",
                Type = JsonType,
                Title = text.GetLocalizedText("api.resetCardTemplate"),
                Fields = [VersionField(character.Version)]
            });
        }

        return [.. actions];
    }

    private SirenField VersionField(long version) =>
        new() { Name = "version", Type = "hidden", Value = SirenJson.ToElement(version), Title = text.GetLocalizedText("skills.version") };

    private SirenField ContentField() =>
        new() { Name = "content", Type = "file", Title = text.GetLocalizedText("cardTemplates.file") };
}
