using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SciFiLarpNetwork.Common.ApiLinkPolicy;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Management.Api;

public sealed class ManagementApiClient(HttpClient httpClient) : IManagementApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly MediaTypeWithQualityHeaderValue SirenMediaType =
        new(SirenMediaTypes.Json);

    public async Task<ApiDiscovery> DiscoverAsync(
        string apiBaseUrl,
        string locale,
        CancellationToken cancellationToken = default)
    {
        var apiRoot = ApiLinkPolicy.CreateApiRoot(apiBaseUrl);
        var rootResource = await GetEntityAsync(apiRoot, locale, cancellationToken);
        var latestVersionLink = rootResource.Links?.FirstOrDefault(link =>
            link.Rel?.Contains(SirenRelations.LatestVersion, StringComparer.Ordinal) == true);

        if (latestVersionLink is null ||
            !ApiLinkPolicy.TryResolve(apiRoot, latestVersionLink.Href, out var latestVersionUri))
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
        var apiRoot = ApiLinkPolicy.CreateApiRoot(apiBaseUrl);
        if (!ApiLinkPolicy.TryResolve(apiRoot, href, out var target))
        {
            throw new InvalidOperationException("The Siren target is outside the configured API origin or path.");
        }

        return GetEntityAsync(target, locale, cancellationToken);
    }

    public async Task<SirenEntity> SubmitActionAsync(
        string apiBaseUrl,
        SirenAction action,
        JsonElement? body,
        string locale,
        CancellationToken cancellationToken = default)
    {
        var apiRoot = ApiLinkPolicy.CreateApiRoot(apiBaseUrl);
        if (string.IsNullOrWhiteSpace(action.Href) || string.IsNullOrWhiteSpace(action.Method))
        {
            throw new InvalidOperationException("The Siren action must declare a target and HTTP method.");
        }

        if (!ApiLinkPolicy.TryResolve(apiRoot, action.Href, out var target))
        {
            throw new InvalidOperationException("The Siren target is outside the configured API origin or path.");
        }

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
                link.Rel?.Contains(SirenRelations.Navigation, StringComparer.Ordinal) == true &&
                !string.IsNullOrWhiteSpace(link.Href) &&
                !string.IsNullOrWhiteSpace(link.Title) &&
                ApiLinkPolicy.TryResolve(apiRoot, link.Href, out _))
            .ToArray()
        ?? [];

    private static bool IsJsonMediaType(string mediaType) =>
        mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
        mediaType.StartsWith("application/", StringComparison.OrdinalIgnoreCase) &&
        mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);

}
