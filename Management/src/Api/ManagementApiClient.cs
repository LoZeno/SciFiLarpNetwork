using System.Net;
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
    private string? _sessionToken;
    private Uri? _sessionApiRoot;
    private Uri? _sessionRenewTarget;
    private Uri? _sessionLogoutTarget;

    public async Task<ManagementSession> SignInAsync(
        string apiBaseUrl,
        string userName,
        string password,
        string locale,
        CancellationToken cancellationToken = default)
    {
        var apiRoot = ApiLinkPolicy.CreateApiRoot(apiBaseUrl);
        RequireSecureSessionTarget(apiRoot);
        var discovery = await DiscoverAsync(apiBaseUrl, locale, cancellationToken);
        var target = ResolveAdvertisedAction(apiRoot, discovery.ApiResource, "administrator-sign-in", "POST");
        using var request = new HttpRequestMessage(HttpMethod.Post, target);
        request.Headers.Accept.Add(SirenMediaType);
        request.Headers.AcceptLanguage.ParseAdd(locale);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { userName, password }, JsonOptions),
            Encoding.UTF8,
            "application/json");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var resource = await ReadEntityAsync(response, cancellationToken);
        var token = ReadBearerHeader(response);
        if (token is null)
        {
            throw new JsonException("The API did not return a session authorization header.");
        }

        var session = ReadSession(resource);
        var renewTarget = ResolveAdvertisedAction(apiRoot, resource, "administrator-renew-session", "POST");
        var logoutTarget = ResolveAdvertisedAction(apiRoot, resource, "administrator-sign-out", "DELETE");
        _sessionToken = token.Parameter;
        _sessionApiRoot = apiRoot;
        _sessionRenewTarget = renewTarget;
        _sessionLogoutTarget = logoutTarget;
        return session;
    }

    public async Task SignOutAsync(
        string apiBaseUrl,
        string locale,
        CancellationToken cancellationToken = default)
    {
        if (_sessionToken is not { } token || _sessionLogoutTarget is not { } target)
        {
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Delete, target);
        request.Headers.Accept.Add(SirenMediaType);
        request.Headers.AcceptLanguage.ParseAdd(locale);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                await ReadEntityAsync(response, cancellationToken);
            }
        }
        finally
        {
            ClearSession();
        }
    }

    public async Task<ManagementSession> RenewSessionAsync(
        string apiBaseUrl,
        string locale,
        CancellationToken cancellationToken = default)
    {
        var token = _sessionToken ?? throw new InvalidOperationException("No administrator session is active.");
        var root = _sessionApiRoot ?? throw new InvalidOperationException("No administrator session is active.");
        var target = _sessionRenewTarget ?? throw new InvalidOperationException("No administrator session is active.");
        var configuredRoot = ApiLinkPolicy.CreateApiRoot(apiBaseUrl);
        if (!string.Equals(root.AbsoluteUri, configuredRoot.AbsoluteUri, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The configured API address differs from the active session.");
        }
        using var request = new HttpRequestMessage(HttpMethod.Post, target);
        request.Headers.Accept.Add(SirenMediaType);
        request.Headers.AcceptLanguage.ParseAdd(locale);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var resource = await ReadEntityAsync(response, cancellationToken);
        var refreshedToken = ReadBearerHeader(response);
        if (refreshedToken is null)
        {
            throw new JsonException("The API did not return a renewed session authorization header.");
        }

        var session = ReadSession(resource);
        var renewTarget = ResolveAdvertisedAction(root, resource, "administrator-renew-session", "POST");
        var logoutTarget = ResolveAdvertisedAction(root, resource, "administrator-sign-out", "DELETE");
        _sessionToken = refreshedToken.Parameter;
        _sessionRenewTarget = renewTarget;
        _sessionLogoutTarget = logoutTarget;
        return session;
    }

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
        AddSessionAuthorization(request);
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
        AddSessionAuthorization(request);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadEntityAsync(response, cancellationToken);
    }

    private async Task<SirenEntity> ReadEntityAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                ClearSession();
            }

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

    private static Uri ResolveAdvertisedAction(Uri apiRoot, SirenEntity resource, string name, string method)
    {
        var action = resource.Actions?.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, name, StringComparison.Ordinal));
        if (action is null ||
            !string.Equals(action.Method, method, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(action.Type, "application/json", StringComparison.OrdinalIgnoreCase) ||
            !ApiLinkPolicy.TryResolve(apiRoot, action.Href, out var target))
        {
            throw new InvalidOperationException("The API did not advertise a safe administrator session action.");
        }

        RequireSecureSessionTarget(target);
        return target;
    }

    private static void RequireSecureSessionTarget(Uri target)
    {
        if (target.Scheme != Uri.UriSchemeHttps && !target.IsLoopback)
        {
            throw new InvalidOperationException("Administrator sessions require HTTPS outside loopback.");
        }
    }

    private void AddSessionAuthorization(HttpRequestMessage request)
    {
        if (_sessionToken is { } token && request.RequestUri is { } target && IsSessionTarget(target))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private bool IsSessionTarget(Uri target) =>
        _sessionApiRoot is { } root && ApiLinkPolicy.TryResolve(root, target.AbsoluteUri, out _);

    private void ClearSession()
    {
        _sessionToken = null;
        _sessionApiRoot = null;
        _sessionRenewTarget = null;
        _sessionLogoutTarget = null;
    }

    private static AuthenticationHeaderValue? ReadBearerHeader(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Authorization", out var values))
        {
            return null;
        }

        var rawValue = values.SingleOrDefault();
        return AuthenticationHeaderValue.TryParse(rawValue, out var header) &&
               string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) &&
               !string.IsNullOrWhiteSpace(header.Parameter)
            ? header
            : null;
    }

    private static ManagementSession ReadSession(SirenEntity resource)
    {
        if (resource.Properties is not { ValueKind: JsonValueKind.Object } properties ||
            !properties.TryGetProperty("userName", out var userName) || userName.ValueKind != JsonValueKind.String ||
            !properties.TryGetProperty("role", out var role) || role.ValueKind != JsonValueKind.String ||
            !properties.TryGetProperty("expiresAtUtc", out var expiresAtUtc) ||
            !expiresAtUtc.TryGetDateTimeOffset(out var expiresAt))
        {
            throw new JsonException("The API returned an invalid administrator session resource.");
        }

        return new ManagementSession(userName.GetString()!, role.GetString()!, expiresAt);
    }

}
