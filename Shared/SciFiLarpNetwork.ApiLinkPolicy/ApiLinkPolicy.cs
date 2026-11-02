namespace SciFiLarpNetwork.Common.ApiLinkPolicy;

public static class ApiLinkPolicy
{
    public static Uri CreateApiRoot(string apiBaseUrl)
    {
        if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var configuredUri) ||
            configuredUri.Scheme is not ("http" or "https") ||
            !string.IsNullOrEmpty(configuredUri.UserInfo) ||
            !string.IsNullOrEmpty(configuredUri.Query) ||
            !string.IsNullOrEmpty(configuredUri.Fragment) ||
            !IsApiPath(configuredUri.AbsolutePath))
        {
            throw new UriFormatException("The API URL must be an absolute HTTP(S) URL under /api.");
        }

        var normalizedPath = configuredUri.AbsolutePath.TrimEnd('/') + "/";
        return new UriBuilder(configuredUri) { Path = normalizedPath }.Uri;
    }

    public static bool TryResolve(Uri apiRoot, string? href, out Uri target)
    {
        target = default!;
        if (string.IsNullOrWhiteSpace(href) ||
            !Uri.TryCreate(apiRoot, href, out var resolved) ||
            !string.Equals(resolved.Scheme, apiRoot.Scheme, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(resolved.Host, apiRoot.Host, StringComparison.OrdinalIgnoreCase) ||
            resolved.Port != apiRoot.Port ||
            !string.IsNullOrEmpty(resolved.UserInfo) ||
            !string.IsNullOrEmpty(resolved.Fragment) ||
            !(resolved.AbsolutePath.Equals(apiRoot.AbsolutePath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase) ||
              resolved.AbsolutePath.StartsWith(apiRoot.AbsolutePath, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        target = resolved;
        return true;
    }

    private static bool IsApiPath(string path) =>
        path.Equals("/api", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith("/api", StringComparison.OrdinalIgnoreCase);
}
