using System.Net;
using System.Text;
using System.Text.Json;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Management.Tests.Api;

public sealed class ManagementApiClientAuthenticationTests
{
    private const string ApiBaseUrl = "http://localhost:5000/api";
    private const string RootResource =
        """{"class":["api"],"links":[{"rel":["self"],"href":"/api"},{"rel":["urn:scifilarnetwork:rels:latest-version"],"href":"/api/v2"}]}""";
    private const string VersionResource =
        """{"class":["api","version"],"links":[{"rel":["self"],"href":"/api/v2"}],"actions":[{"name":"administrator-sign-in","href":"/api/v2/auth/login","method":"POST","type":"application/json","fields":[{"name":"userName"},{"name":"password","type":"password"}]}]}""";
    private const string SessionResource =
        """{"class":["administrator-session"],"properties":{"accountId":"a78d4ce3-cf72-47de-a7b8-66f9a5d229fb","userName":"operator","role":"administrator","expiresAtUtc":"2026-11-05T06:25:00Z"},"links":[{"rel":["self"],"href":"/api/v2/auth/session"}],"actions":[{"name":"administrator-renew-session","href":"/api/v2/auth/refresh","method":"POST","type":"application/json"},{"name":"administrator-sign-out","href":"/api/v2/auth/logout","method":"DELETE","type":"application/json"}]}""";
    private const string Resource = """{"class":["resource"],"properties":{}}""";

    [Fact]
    public async Task SignInUsesResponseHeaderAndProtectedRequestsAttachToken()
    {
        var handler = new RecordingHandler((request, _) => Task.FromResult(
            request.Method == HttpMethod.Post
                ? CreateSirenResponse(SessionResource, "session-token")
                : CreateSirenResponse(Resource)));
        using var client = new HttpClient(handler);
        var apiClient = new ManagementApiClient(client);

        var session = await apiClient.SignInAsync(ApiBaseUrl, "operator", "not-persisted-password", "en");
        await apiClient.GetResourceAsync(ApiBaseUrl, "/api/v1/admin/accounts", "en");

        Assert.Equal("operator", session.UserName);
        Assert.Equal("administrator", session.Role);
        Assert.Equal("http://localhost:5000/api/v2/auth/login", handler.Requests[2].Uri?.ToString());
        using var requestBody = JsonDocument.Parse(handler.Requests[2].Body!);
        Assert.Equal(
            "not-persisted-password",
            requestBody.RootElement.GetProperty("password").GetString());
        Assert.Null(handler.Requests[2].Authorization);
        Assert.Equal("Bearer session-token", handler.Requests[3].Authorization);
    }

    [Fact]
    public async Task RenewSessionRotatesTokenAndLogoutClearsIt()
    {
        var handler = new RecordingHandler((request, _) => Task.FromResult(
            request.Method switch
            {
                var method when method == HttpMethod.Post &&
                                request.RequestUri?.AbsolutePath.EndsWith("/refresh", StringComparison.Ordinal) == true =>
                    CreateSirenResponse(SessionResource, "renewed-token"),
                var method when method == HttpMethod.Post => CreateSirenResponse(SessionResource, "initial-token"),
                var method when method == HttpMethod.Delete => new HttpResponseMessage(HttpStatusCode.NoContent),
                _ => CreateSirenResponse(Resource)
            }));
        using var client = new HttpClient(handler);
        var apiClient = new ManagementApiClient(client);
        await apiClient.SignInAsync(ApiBaseUrl, "operator", "local-password", "en");

        await apiClient.RenewSessionAsync(ApiBaseUrl, "en");
        await apiClient.GetResourceAsync(ApiBaseUrl, "/api/v1/admin/accounts", "en");
        await apiClient.SignOutAsync(ApiBaseUrl, "en");
        await apiClient.GetResourceAsync(ApiBaseUrl, "/api/v1/admin/accounts", "en");

        Assert.Equal("Bearer initial-token", handler.Requests[3].Authorization);
        Assert.Equal("Bearer renewed-token", handler.Requests[4].Authorization);
        Assert.Equal("Bearer renewed-token", handler.Requests[5].Authorization);
        Assert.Null(handler.Requests[6].Authorization);
    }

    [Fact]
    public async Task UnauthorizedResponseClearsSessionToken()
    {
        var resourceRequests = 0;
        var handler = new RecordingHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Post)
            {
                return Task.FromResult(CreateSirenResponse(SessionResource, "expired-token"));
            }

            if (Interlocked.Increment(ref resourceRequests) == 1)
            {
                return Task.FromResult(CreateSirenResponse(
                    """{"class":["error"],"properties":{"code":"errors.unauthorized","message":"Unauthorized"}}""",
                    statusCode: HttpStatusCode.Unauthorized));
            }

            return Task.FromResult(CreateSirenResponse(Resource));
        });
        using var client = new HttpClient(handler);
        var apiClient = new ManagementApiClient(client);
        await apiClient.SignInAsync(ApiBaseUrl, "operator", "local-password", "en");

        await Assert.ThrowsAsync<ManagementApiException>(() =>
            apiClient.GetResourceAsync(ApiBaseUrl, "/api/v1/admin/accounts", "en"));
        await apiClient.GetResourceAsync(ApiBaseUrl, "/api/v1/admin/accounts", "en");

        Assert.Equal("Bearer expired-token", handler.Requests[3].Authorization);
        Assert.Null(handler.Requests[4].Authorization);
    }

    [Fact]
    public async Task SignInRejectsRemoteCleartextBeforeSendingCredentials()
    {
        var handler = new RecordingHandler((_, _) => Task.FromResult(CreateSirenResponse(SessionResource, "token")));
        using var client = new HttpClient(handler);
        var apiClient = new ManagementApiClient(client);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            apiClient.SignInAsync("http://remote.example/api", "operator", "password", "en"));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SessionTokenIsBoundToTheApiThatIssuedIt()
    {
        var handler = new RecordingHandler((request, _) => Task.FromResult(
            request.Method == HttpMethod.Post
                ? CreateSirenResponse(SessionResource, "original-token")
                : request.Method == HttpMethod.Delete
                    ? new HttpResponseMessage(HttpStatusCode.NoContent)
                    : CreateSirenResponse(Resource)));
        using var client = new HttpClient(handler);
        var apiClient = new ManagementApiClient(client);
        await apiClient.SignInAsync(ApiBaseUrl, "operator", "local-password", "en");

        await apiClient.GetResourceAsync("https://other.example/api", "/api/v1/admin/accounts", "en");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            apiClient.RenewSessionAsync("https://other.example/api", "en"));
        await apiClient.SignOutAsync("https://other.example/api", "en");

        Assert.Null(handler.Requests[3].Authorization);
        Assert.Equal("http://localhost:5000/api/v2/auth/logout", handler.Requests[4].Uri?.ToString());
        Assert.Equal("Bearer original-token", handler.Requests[4].Authorization);
    }

    [Fact]
    public async Task SignInRejectsAdvertisedCrossOriginActionBeforeSendingCredentials()
    {
        const string unsafeVersion =
            """{"class":["api","version"],"actions":[{"name":"administrator-sign-in","href":"https://other.example/login","method":"POST","type":"application/json"}]}""";
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(CreateSirenResponse(SessionResource, "token")),
            unsafeVersion);
        using var client = new HttpClient(handler);
        var apiClient = new ManagementApiClient(client);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            apiClient.SignInAsync(ApiBaseUrl, "operator", "local-password", "en"));

        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request => Assert.Equal(HttpMethod.Get, request.Method));
    }

    private static HttpResponseMessage CreateSirenResponse(
        string body,
        string? token = null,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, SirenMediaTypes.Json)
        };
        if (token is not null)
        {
            response.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
        }

        return response;
    }

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder,
        string versionResource = VersionResource) : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri,
                request.Headers.Authorization?.ToString(),
                request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));
            var path = request.RequestUri?.AbsolutePath.TrimEnd('/');
            if (request.Method == HttpMethod.Get && path == "/api")
            {
                return CreateSirenResponse(RootResource);
            }

            if (request.Method == HttpMethod.Get && path == "/api/v2")
            {
                return CreateSirenResponse(versionResource);
            }

            return await responder(request, cancellationToken);
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, Uri? Uri, string? Authorization, string? Body);
}