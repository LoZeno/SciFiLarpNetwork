using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SciFiLarpNetwork.Management.Api.Siren;

namespace SciFiLarpNetwork.Management.Api;

public sealed class ManagementApiClient(HttpClient httpClient) : IManagementApiClient, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly MediaTypeWithQualityHeaderValue SirenMediaType =
        new("application/vnd.siren+json");

    public async Task<ApiDiscovery> DiscoverAsync(
        string apiBaseUrl,
        string locale,
        CancellationToken cancellationToken = default)
    {
        var apiRoot = CreateApiRoot(apiBaseUrl);
        var rootResource = await GetEntityAsync(apiRoot, locale, cancellationToken);
        var latestVersionLink = rootResource.Links?.FirstOrDefault(link =>
            link.Rel?.Contains("urn:scifilarnetwork:rels:latest-version", StringComparer.Ordinal) == true);

        if (latestVersionLink is null ||
            !TryResolveApiTarget(apiRoot, latestVersionLink.Href, out var latestVersionUri))
        {
            return new ApiDiscovery(rootResource, GetSafeNavigationLinks(apiRoot, rootResource));
        }

        var versionResource = await GetEntityAsync(latestVersionUri, locale, cancellationToken);
        return new ApiDiscovery(versionResource, GetSafeNavigationLinks(apiRoot, versionResource));
    }

    public Task<SirenEntity> GetResourceAsync(
        string apiBaseUrl,
        string href,
        string locale,
        CancellationToken cancellationToken = default)
    {
        var apiRoot = CreateApiRoot(apiBaseUrl);
        var target = ResolveApiTarget(apiRoot, href);
        return GetEntityAsync(target, locale, cancellationToken);
    }

    public async Task<SirenEntity> SubmitActionAsync(
        string apiBaseUrl,
        SirenAction action,
        JsonElement? body,
        string locale,
        CancellationToken cancellationToken = default)
    {
        var apiRoot = CreateApiRoot(apiBaseUrl);
        if (string.IsNullOrWhiteSpace(action.Href) || string.IsNullOrWhiteSpace(action.Method))
        {
            throw new InvalidOperationException("The Siren action must declare a target and HTTP method.");
        }

        var target = ResolveApiTarget(apiRoot, action.Href);
        var method = action.Method.ToUpperInvariant() switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "PATCH" => HttpMethod.Patch,
            "DELETE" => HttpMethod.Delete,
            _ => throw new InvalidOperationException($"Unsupported Siren action method '{action.Method}'.")
        };

        if (body.HasValue && !string.Equals(
                action.Type,
                "application/json",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only application/json Siren action bodies are supported.");
        }

        if (body.HasValue && method == HttpMethod.Get)
        {
            throw new InvalidOperationException("GET Siren actions cannot include a request body.");
        }

        using var request = new HttpRequestMessage(method, target);
        request.Headers.Accept.Add(SirenMediaType);
        request.Headers.AcceptLanguage.ParseAdd(locale);
        if (body is { } jsonBody)
        {
            request.Content = new StringContent(
                jsonBody.GetRawText(),
                Encoding.UTF8,
                "application/json");
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadEntityAsync(response, cancellationToken);
    }

    private async Task<SirenEntity> GetEntityAsync(
        Uri target,
        string locale,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, target);
        request.Headers.Accept.Add(SirenMediaType);
        request.Headers.AcceptLanguage.ParseAdd(locale);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadEntityAsync(response, cancellationToken);
    }

    private static async Task<SirenEntity> ReadEntityAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorCode = await ReadErrorCodeAsync(response, cancellationToken);
            throw new ManagementApiException(errorCode, response.StatusCode);
        }

        if (response.Content.Headers.ContentType?.MediaType is not { } mediaType ||
            !IsJsonMediaType(mediaType))
        {
            throw new JsonException("The API response did not use a JSON media type.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<SirenEntity>(
                stream,
                JsonOptions,
                cancellationToken)
            ?? throw new JsonException("The API returned an empty Siren resource.");
    }

    private static async Task<string> ReadErrorCodeAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("properties", out var properties) &&
                properties.ValueKind == JsonValueKind.Object &&
                properties.TryGetProperty("code", out var code) &&
                code.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(code.GetString()))
            {
                return code.GetString()!;
            }
        }
        catch (JsonException)
        {
            // Non-Siren error responses are represented by their HTTP status instead.
        }

        return $"http.{(int)response.StatusCode}";
    }

    private static SirenLink[] GetSafeNavigationLinks(Uri apiRoot, SirenEntity resource) =>
        resource.Links?
            .Where(link =>
                link.Rel?.Contains("urn:scifilarnetwork:rels:navigation", StringComparer.Ordinal) == true &&
                !string.IsNullOrWhiteSpace(link.Href) &&
                !string.IsNullOrWhiteSpace(link.Title) &&
                TryResolveApiTarget(apiRoot, link.Href, out _))
            .ToArray()
        ?? [];

    private static Uri CreateApiRoot(string apiBaseUrl)
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

    private static bool IsApiPath(string path) =>
        path.Equals("/api", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);

    private static bool IsJsonMediaType(string mediaType) =>
        mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
        mediaType.StartsWith("application/", StringComparison.OrdinalIgnoreCase) &&
        mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);

    private static Uri ResolveApiTarget(Uri apiRoot, string? href)
    {
        if (TryResolveApiTarget(apiRoot, href, out var target))
        {
            return target;
        }

        throw new InvalidOperationException("The Siren target is outside the configured API origin or path.");
    }

    private static bool TryResolveApiTarget(Uri apiRoot, string? href, out Uri target)
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

    public void Dispose() => httpClient.Dispose();
}
