using SciFiLarpNetwork.Backend.Application.Networks;
using SciFiLarpNetwork.Backend.Domain.Networks;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.Networks;

/// <summary>
/// Builds the Siren representations of the network catalog. Write actions are advertised only when
/// <paramref name="canWrite"/> is set and the action applies to the network's state; the server still authorizes
/// and validates every request.
/// </summary>
internal sealed class NetworkResources(IApplicationText text, string pathBase, bool canWrite)
{
    private const string JsonType = "application/json";

    public string NetworksPath => $"{pathBase}/api/v1/networks";

    public string NetworkPath(Guid networkId) => $"{NetworksPath}/{networkId:D}";

    public string ImagePath(Guid imageId) => $"{pathBase}/api/v1/network-images/{imageId:D}";

    public SirenEntity<NetworkCollectionProperties> NetworkCollection(IReadOnlyList<NetworkSummary> networks, bool includeDisabled) =>
        new()
        {
            Class = ["network-collection"],
            Properties = new NetworkCollectionProperties(
                text.GetLocalizedText("api.networks"),
                text.GetLocalizedText("api.networksDescription"),
                includeDisabled,
                networks.Any(network => network.IsDefault),
                Enum.GetValues<NetworkTheme>()
                    .Select(theme => new NetworkThemeOption(theme, text.GetLocalizedText(ThemeTitleKey(theme))))
                    .ToArray()),
            Entities = networks.Select(network => new SirenSubEntity
            {
                Rel = ["item"],
                Class = ["network"],
                Href = NetworkPath(network.NetworkId),
                Properties = SirenJson.ToElement(network),
                Links = ImageLinks(network)
            }).ToArray(),
            Links =
            [
                new SirenLink { Rel = ["self"], Href = includeDisabled ? WithDisabled(NetworksPath) : NetworksPath },
                new SirenLink { Rel = [SirenRelations.WithDisabled], Href = WithDisabled(NetworksPath) },
                new SirenLink { Rel = [SirenRelations.WithoutDisabled], Href = NetworksPath }
            ],
            Actions = canWrite
                ?
                [
                    new SirenAction
                    {
                        Name = "create-network",
                        Href = NetworksPath,
                        Method = "POST",
                        Type = JsonType,
                        Title = text.GetLocalizedText("api.createNetwork"),
                        Fields =
                        [
                            Field("code", "text", "networks.code", (string?)null),
                            .. DetailFields(null)
                        ]
                    }
                ]
                : null
        };

    public SirenEntity<NetworkSummary> Network(NetworkSummary network) =>
        new()
        {
            Class = ["network"],
            Properties = network,
            Links =
            [
                new SirenLink { Rel = ["self"], Href = NetworkPath(network.NetworkId) },
                new SirenLink { Rel = ["collection"], Href = NetworksPath },
                .. ImageLinks(network) ?? []
            ],
            Actions = canWrite ? NetworkActions(network) : null
        };

    public static string ThemeTitleKey(NetworkTheme theme)
    {
        var name = theme.ToString();
        return $"networks.theme.{char.ToLowerInvariant(name[0])}{name[1..]}";
    }

    private static string WithDisabled(string path) => $"{path}?includeDisabled=true";

    private SirenLink[]? ImageLinks(NetworkSummary network) =>
        network.BackgroundImage is { } image
            ? [new SirenLink { Rel = [SirenRelations.BackgroundImage], Href = ImagePath(image.ImageId), Type = image.MediaType }]
            : null;

    private SirenAction[] NetworkActions(NetworkSummary network)
    {
        var path = NetworkPath(network.NetworkId);
        var actions = new List<SirenAction>
        {
            new()
            {
                Name = "update-network",
                Href = path,
                Method = "PUT",
                Type = JsonType,
                Title = text.GetLocalizedText("api.updateNetwork"),
                Fields = [VersionField(network.Version), .. DetailFields(network)]
            },
            new()
            {
                Name = "set-background-image",
                Href = $"{path}/background-image",
                Method = "PUT",
                Type = JsonType,
                Title = text.GetLocalizedText("api.setBackgroundImage"),
                Fields = [VersionField(network.Version), Field("content", "file", "networks.backgroundImage", (string?)null)]
            }
        };

        if (network.BackgroundImage is not null)
        {
            actions.Add(new SirenAction
            {
                Name = "remove-background-image",
                Href = $"{path}/background-image",
                Method = "DELETE",
                Type = JsonType,
                Title = text.GetLocalizedText("api.removeBackgroundImage"),
                Fields = [VersionField(network.Version)]
            });
        }

        if (network.IsEnabled && !network.IsDefault)
        {
            actions.Add(SimpleAction("make-default-network", $"{path}/make-default", "api.makeDefaultNetwork"));
        }

        if (!network.IsEnabled)
        {
            actions.Add(SimpleAction("enable-network", $"{path}/enable", "api.enableNetwork"));
        }
        else if (!network.IsDefault)
        {
            actions.Add(SimpleAction("disable-network", $"{path}/disable", "api.disableNetwork"));
        }

        return [.. actions];
    }

    private SirenAction SimpleAction(string name, string href, string titleKey) =>
        new() { Name = name, Href = href, Method = "POST", Type = JsonType, Title = text.GetLocalizedText(titleKey) };

    private SirenField VersionField(long version) =>
        new() { Name = "version", Type = "hidden", Value = SirenJson.ToElement(version), Title = text.GetLocalizedText("skills.version") };

    private SirenField[] DetailFields(NetworkSummary? network) =>
    [
        Field("name", "text", "skills.name", network?.Name),
        Field("description", "textarea", "skills.description", network?.Description),
        Field("theme", "radio", "networks.theme", network?.Theme)
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
