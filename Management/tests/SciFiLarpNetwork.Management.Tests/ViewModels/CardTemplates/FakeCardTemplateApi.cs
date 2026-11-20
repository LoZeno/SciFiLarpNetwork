using System.Net;
using System.Text.Json;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.CardTemplates;

/// <summary>An in-memory character-card template that answers with the same Siren shape as the backend.</summary>
internal sealed class FakeCardTemplateApi : IManagementApiClient
{
    public const string TemplatesHref = "/api/v1/card-templates";
    public const string TemplateHref = "/api/v1/card-templates/character";
    public static readonly byte[] PreviewPdf = "%PDF-preview"u8.ToArray();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public bool CanWrite { get; init; } = true;

    public byte[] Source { get; set; } = "default template"u8.ToArray();

    public bool IsDefault { get; set; } = true;

    public string? UploadedBy { get; set; }

    public DateTimeOffset? UploadedAtUtc { get; set; }

    public long Version { get; set; }

    public List<Submission> Submissions { get; } = [];

    /// <summary>One-shot failures for the next submission or download of the named action.</summary>
    public Dictionary<string, Exception> Failures { get; } = [];

    public int TemplateLoads { get; private set; }

    public Task<SirenEntity> GetResourceAsync(string apiBaseUrl, string href, string locale, CancellationToken cancellationToken = default)
    {
        if (href == TemplateHref)
        {
            TemplateLoads++;
        }

        return Task.FromResult(href switch
        {
            TemplatesHref => new SirenEntity
            {
                Class = ["card-template-collection"],
                Properties = Json(new { title = "Card templates", description = "" }),
                Entities = [new SirenSubEntity { Rel = ["item"], Href = TemplateHref, Properties = Properties() }]
            },
            TemplateHref => Template(),
            _ => throw new ManagementApiException("errors.notFound", HttpStatusCode.NotFound)
        });
    }

    public Task<byte[]> GetFileAsync(string apiBaseUrl, string href, string mediaType, string locale,
        CancellationToken cancellationToken = default) =>
        href == $"{TemplateHref}/source" && mediaType == "text/plain"
            ? Task.FromResult(Source)
            : Task.FromException<byte[]>(new ManagementApiException("errors.notFound", HttpStatusCode.NotFound));

    public Task<byte[]> DownloadActionAsync(string apiBaseUrl, SirenAction action, JsonElement body, string mediaType, string locale,
        CancellationToken cancellationToken = default)
    {
        Submissions.Add(new Submission(action.Name, body.GetRawText()));
        return Failures.Remove(action.Name, out var failure)
            ? Task.FromException<byte[]>(failure)
            : Task.FromResult(PreviewPdf);
    }

    public Task<SirenEntity> SubmitActionAsync(string apiBaseUrl, SirenAction action, JsonElement? body, string locale,
        CancellationToken cancellationToken = default)
    {
        Submissions.Add(new Submission(action.Name, body?.GetRawText()));
        if (Failures.Remove(action.Name, out var failure))
        {
            return Task.FromException<SirenEntity>(failure);
        }

        Version++;
        IsDefault = action.Name == "reset-card-template";
        UploadedBy = IsDefault ? null : "local-admin";
        UploadedAtUtc = IsDefault ? null : new DateTimeOffset(2026, 11, 20, 10, 30, 0, TimeSpan.Zero);
        return Task.FromResult(Template());
    }

    public Task<byte[]> GetImageAsync(string apiBaseUrl, string href, string locale, CancellationToken cancellationToken = default) =>
        Task.FromException<byte[]>(new NotSupportedException("This fake serves no images."));

    public Task<ManagementSession> SignInAsync(string apiBaseUrl, string userName, string password, string locale,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ManagementSession(userName, "administrator", DateTimeOffset.UtcNow.AddHours(1)));

    public Task SignOutAsync(string apiBaseUrl, string locale, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<ManagementSession> RenewSessionAsync(string apiBaseUrl, string locale, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ManagementSession("operator", "administrator", DateTimeOffset.UtcNow.AddHours(1)));

    public Task<ApiDiscovery> DiscoverAsync(string apiBaseUrl, string locale, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ApiDiscovery(
            new SirenEntity { Class = ["api"], Properties = Json(new { title = "Administration" }) },
            [new SirenLink { Rel = [SirenRelations.Navigation, SirenRelations.CardTemplates], Href = TemplatesHref, Title = "Card templates" }]));

    private SirenEntity Template()
    {
        List<SirenAction> actions =
        [
            Action("upload-card-template", TemplateHref, "PUT"),
            Action("preview-card-template", $"{TemplateHref}/preview", "POST")
        ];
        if (!IsDefault)
        {
            actions.Add(Action("reset-card-template", $"{TemplateHref}/reset", "POST"));
        }

        return new SirenEntity
        {
            Class = ["card-template"],
            Properties = Properties(),
            Links =
            [
                new SirenLink { Rel = ["self"], Href = TemplateHref },
                new SirenLink { Rel = [SirenRelations.TemplateSource], Href = $"{TemplateHref}/source", Type = "text/plain" }
            ],
            Actions = CanWrite ? actions : null
        };
    }

    private JsonElement Properties() =>
        Json(new
        {
            kind = "character",
            title = "Character cards",
            isDefault = IsDefault,
            uploadedBy = UploadedBy,
            uploadedAtUtc = UploadedAtUtc,
            version = Version
        });

    private static SirenAction Action(string name, string href, string method) =>
        new() { Name = name, Href = href, Method = method, Type = "application/json" };

    private static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value, JsonOptions);
}
