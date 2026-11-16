using System.Net;
using System.Text.Json;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Networks;

/// <summary>
/// An in-memory network catalog that answers with the same Siren shape, links and actions as the backend.
/// </summary>
internal sealed class FakeNetworkApi : IManagementApiClient
{
    public const string NetworksHref = "/api/v1/networks";
    private const string ImagesHref = "/api/v1/network-images";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] ThemeNames = ["blue", "red", "green", "yellow", "amber", "grey", "purple"];

    public bool CanWrite { get; init; } = true;

    public List<FakeNetwork> Networks { get; } = [];

    public Dictionary<Guid, byte[]> Images { get; } = [];

    public List<Submission> Submissions { get; } = [];

    /// <summary>One-shot failures for the next submission of the named action.</summary>
    public Dictionary<string, Exception> SubmitFailures { get; } = [];

    public Exception? GetFailure { get; set; }

    /// <summary>When set, every image download fails with this exception.</summary>
    public Exception? ImageFailure { get; init; }

    public FakeNetwork AddNetwork(string code, string name, bool isEnabled = true, bool isDefault = false)
    {
        var network = new FakeNetwork(Guid.NewGuid(), code, name) { IsEnabled = isEnabled, IsDefault = isDefault };
        Networks.Add(network);
        return network;
    }

    public Guid AddImage(FakeNetwork network, byte[] content)
    {
        var imageId = Guid.NewGuid();
        Images[imageId] = content;
        network.ImageId = imageId;
        return imageId;
    }

    public Task<SirenEntity> GetResourceAsync(string apiBaseUrl, string href, string locale, CancellationToken cancellationToken = default)
    {
        if (GetFailure is { } failure)
        {
            return Task.FromException<SirenEntity>(failure);
        }

        var parts = href.Split('?');
        var includeDisabled = parts.Length > 1 && parts[1] == "includeDisabled=true";
        var segments = parts[0].Split('/', StringSplitOptions.RemoveEmptyEntries);
        return Task.FromResult(segments switch
        {
            ["api", "v1", "networks"] => Collection(includeDisabled),
            ["api", "v1", "networks", var id] => Entity(Find(id)),
            _ => throw new ManagementApiException("errors.notFound", HttpStatusCode.NotFound)
        });
    }

    public Task<byte[]> GetImageAsync(string apiBaseUrl, string href, string locale, CancellationToken cancellationToken = default) =>
        ImageFailure is { } failure
            ? Task.FromException<byte[]>(failure)
            : Images.TryGetValue(Guid.Parse(href.Split('/')[^1]), out var image)
                ? Task.FromResult(image)
                : Task.FromException<byte[]>(new ManagementApiException("errors.notFound", HttpStatusCode.NotFound));

    public Task<SirenEntity> SubmitActionAsync(
        string apiBaseUrl,
        SirenAction action,
        JsonElement? body,
        string locale,
        CancellationToken cancellationToken = default)
    {
        Submissions.Add(new Submission(action.Name, body?.GetRawText()));
        return SubmitFailures.Remove(action.Name, out var failure)
            ? Task.FromException<SirenEntity>(failure)
            : Task.FromResult(Apply(action, body));
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
            [new SirenLink { Rel = [SirenRelations.Navigation, SirenRelations.Networks], Href = NetworksHref, Title = "Networks" }]));

    private SirenEntity Apply(SirenAction action, JsonElement? body)
    {
        var segments = action.Href.Split('/', StringSplitOptions.RemoveEmptyEntries);
        switch (action.Name)
        {
            case "create-network":
                var created = new FakeNetwork(Guid.NewGuid(), body!.Value.GetProperty("code").GetString()!, string.Empty);
                ApplyDetails(created, body.Value);
                Networks.Add(created);
                return Entity(created);
            case "update-network":
                var network = Find(segments[3]);
                RequireVersion(body!.Value, network);
                ApplyDetails(network, body.Value);
                network.Version++;
                return Entity(network);
            case "set-background-image":
                var withImage = Find(segments[3]);
                RequireVersion(body!.Value, withImage);
                AddImage(withImage, body.Value.GetProperty("content").GetBytesFromBase64());
                withImage.Version++;
                return Entity(withImage);
            case "remove-background-image":
                var withoutImage = Find(segments[3]);
                RequireVersion(body!.Value, withoutImage);
                withoutImage.ImageId = null;
                withoutImage.Version++;
                return Entity(withoutImage);
            case "enable-network" or "disable-network":
                var toggled = Find(segments[3]);
                toggled.IsEnabled = action.Name == "enable-network";
                toggled.Version++;
                return Entity(toggled);
            case "make-default-network":
                var chosen = Find(segments[3]);
                foreach (var other in Networks.Where(item => item.IsDefault))
                {
                    other.IsDefault = false;
                }

                chosen.IsDefault = true;
                chosen.Version++;
                return Entity(chosen);
            default:
                throw new ManagementApiException("errors.notFound", HttpStatusCode.NotFound);
        }
    }

    private static void ApplyDetails(FakeNetwork network, JsonElement body)
    {
        network.Name = body.GetProperty("name").GetString()!;
        network.Description = body.TryGetProperty("description", out var description) ? description.GetString() : null;
        network.Theme = body.GetProperty("theme").GetString()!;
    }

    private static void RequireVersion(JsonElement body, FakeNetwork network)
    {
        if (body.GetProperty("version").GetInt64() != network.Version)
        {
            throw new ManagementApiException("errors.staleVersion", HttpStatusCode.Conflict);
        }
    }

    private FakeNetwork Find(string id) =>
        Networks.SingleOrDefault(network => network.Id == Guid.Parse(id))
        ?? throw new ManagementApiException("errors.notFound", HttpStatusCode.NotFound);

    private SirenEntity Collection(bool includeDisabled) =>
        new()
        {
            Class = ["network-collection"],
            Properties = Json(new
            {
                title = "Networks",
                includesDisabled = includeDisabled,
                hasDefault = Networks.Any(network => network.IsDefault),
                themes = ThemeNames.Select(name => new { name, title = char.ToUpperInvariant(name[0]) + name[1..] }).ToArray()
            }),
            Entities = Networks
                .Where(network => includeDisabled || network.IsEnabled)
                .OrderBy(network => network.Name, StringComparer.OrdinalIgnoreCase)
                .Select(network => new SirenSubEntity
                {
                    Rel = ["item"],
                    Href = Href(network),
                    Properties = Properties(network),
                    Links = ImageLinks(network)
                })
                .ToArray(),
            Links =
            [
                new SirenLink { Rel = [SirenRelations.WithDisabled], Href = $"{NetworksHref}?includeDisabled=true" },
                new SirenLink { Rel = [SirenRelations.WithoutDisabled], Href = NetworksHref }
            ],
            Actions = CanWrite ? [Action("create-network", NetworksHref, "POST")] : null
        };

    private SirenEntity Entity(FakeNetwork network)
    {
        var actions = new List<SirenAction>
        {
            Action("update-network", Href(network), "PUT"),
            Action("set-background-image", $"{Href(network)}/background-image", "PUT")
        };
        if (network.ImageId is not null)
        {
            actions.Add(Action("remove-background-image", $"{Href(network)}/background-image", "DELETE"));
        }

        if (network is { IsEnabled: true, IsDefault: false })
        {
            actions.Add(Action("make-default-network", $"{Href(network)}/make-default", "POST"));
        }

        if (!network.IsEnabled)
        {
            actions.Add(Action("enable-network", $"{Href(network)}/enable", "POST"));
        }
        else if (!network.IsDefault)
        {
            actions.Add(Action("disable-network", $"{Href(network)}/disable", "POST"));
        }

        return new SirenEntity
        {
            Class = ["network"],
            Properties = Properties(network),
            Links = [new SirenLink { Rel = ["self"], Href = Href(network) }, .. ImageLinks(network) ?? []],
            Actions = CanWrite ? actions : null
        };
    }

    private static SirenLink[]? ImageLinks(FakeNetwork network) =>
        network.ImageId is { } imageId
            ? [new SirenLink { Rel = [SirenRelations.BackgroundImage], Href = $"{ImagesHref}/{imageId:D}" }]
            : null;

    private static SirenAction Action(string name, string href, string method) =>
        new() { Name = name, Href = href, Method = method, Type = "application/json" };

    private static string Href(FakeNetwork network) => $"{NetworksHref}/{network.Id:D}";

    private static JsonElement Properties(FakeNetwork network) =>
        Json(new
        {
            networkId = network.Id,
            code = network.Code,
            name = network.Name,
            description = network.Description,
            theme = network.Theme,
            backgroundImage = network.ImageId is { } imageId ? new { imageId, mediaType = "image/png", byteCount = 1 } : null,
            isEnabled = network.IsEnabled,
            isDefault = network.IsDefault,
            version = network.Version
        });

    private static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value, JsonOptions);
}

internal sealed class FakeNetwork(Guid id, string code, string name)
{
    public Guid Id { get; } = id;

    public string Code { get; } = code;

    public string Name { get; set; } = name;

    public string? Description { get; set; }

    public string Theme { get; set; } = "blue";

    public Guid? ImageId { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool IsDefault { get; set; }

    public long Version { get; set; } = 1;
}
