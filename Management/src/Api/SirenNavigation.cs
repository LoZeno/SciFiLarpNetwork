using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Management.Api;

internal static class SirenNavigation
{
    extension(SirenEntity entity)
    {
        public string? FindLinkHref(string rel) => FindHref(entity.Links, rel);

        public SirenAction? FindAction(string name) =>
            entity.Actions?.FirstOrDefault(action => string.Equals(action.Name, name, StringComparison.Ordinal));
    }

    extension(SirenSubEntity entity)
    {
        public string? FindLinkHref(string rel) => FindHref(entity.Links, rel);
    }

    private static string? FindHref(IReadOnlyList<SirenLink>? links, string rel) =>
        links?.FirstOrDefault(link => link.Rel.Contains(rel, StringComparer.Ordinal))?.Href;
}
