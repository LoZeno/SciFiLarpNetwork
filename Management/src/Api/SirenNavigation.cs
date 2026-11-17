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

    extension(SirenAction action)
    {
        /// <summary>
        /// Builds the target of a GET action: Siren sends a GET action's fields as query parameters. Only fields the
        /// action declares are sent; blank values are left out.
        /// </summary>
        public string ToQueryHref(IReadOnlyDictionary<string, string?> values)
        {
            if (!string.Equals(action.Method, "GET", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(action.Href))
            {
                throw new InvalidOperationException("Only GET Siren actions take query parameters.");
            }

            var declared = (action.Fields ?? []).Select(field => field.Name).ToHashSet(StringComparer.Ordinal);
            if (values.Keys.FirstOrDefault(name => !declared.Contains(name)) is { } unknown)
            {
                throw new InvalidOperationException($"The Siren action does not declare the field '{unknown}'.");
            }

            var parameters = values
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
                .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!.Trim())}")
                .ToArray();
            return parameters.Length == 0 ? action.Href : $"{action.Href}?{string.Join('&', parameters)}";
        }
    }

    extension(SirenSubEntity entity)
    {
        public string? FindLinkHref(string rel) => FindHref(entity.Links, rel);
    }

    private static string? FindHref(IReadOnlyList<SirenLink>? links, string rel) =>
        links?.FirstOrDefault(link => link.Rel.Contains(rel, StringComparer.Ordinal))?.Href;
}
