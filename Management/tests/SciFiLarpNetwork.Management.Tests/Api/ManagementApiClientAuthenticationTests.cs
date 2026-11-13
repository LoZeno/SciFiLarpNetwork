using System.Net;
using System.Text;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;

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
    public async Task SignInReturnsTheAdvertisedSessionRecord()
    {
        var handler = CreateStandardHandler("session-token");
        using var client = new HttpClient(handler);
        var apiClient = new ManagementApiClient(client);

        var session = await apiClient.SignInAsync(ApiBaseUrl, "operator", "local-password", "en", TestContext.Current.CancellationToken);

        Assert.Equal(
            new ManagementSession("operator", "administrator", new DateTimeOffset(2026, 11, 5, 6, 25, 0, TimeSpan.Zero)),
            session);
    }

    [Fact]
    public async Task SignInPostsCredentialsToTheAdvertisedLoginAction()
    {
        var handler = CreateStandardHandler("session-token");
        using var client = new HttpClient(handler);
        var apiClient = new ManagementApiClient(client);
        await apiClient.SignInAsync(ApiBaseUrl, "operator", "not-persisted-password", "en", TestContext.Current.CancellationToken);

        Assert.Equal(
            new RecordedRequest(
                HttpMethod.Post,
                new Uri("http://localhost:5000/api/v2/auth/login"),
                null,
                """{"userName":"operator","password":"not-persisted-password"}"""),
            handler.Requests[2]);
    }

    [Fact]
    public async Task ProtectedRequestAttachesTheSessionToken()
    {
        var handler = CreateStandardHandler("session-token");
        var (client, apiClient) = await CreateSignedInClient(handler);
        using (client)
        {
            await apiClient.GetResourceAsync(ApiBaseUrl, "/api/v1/admin/accounts", "en", TestContext.Current.CancellationToken);

            Assert.Equal(
                new RecordedRequest(
                    HttpMethod.Get,
                    new Uri("http://localhost:5000/api/v1/admin/accounts"),
                    "Bearer session-token",
                    null),
                handler.Requests[3]);
        }
    }

    [Fact]
    public async Task RenewSessionSendsTheCurrentSessionToken()
    {
        var handler = CreateRenewalHandler();
        var (client, apiClient) = await CreateSignedInClient(handler);
        using (client)
        {
            await apiClient.RenewSessionAsync(ApiBaseUrl, "en", TestContext.Current.CancellationToken);

            Assert.Equal(
                new RecordedRequest(
                    HttpMethod.Post,
                    new Uri("http://localhost:5000/api/v2/auth/refresh"),
                    "Bearer initial-token",
                    null),
                handler.Requests[3]);
        }
    }

    [Fact]
    public async Task ProtectedRequestAfterRenewalUsesTheRotatedToken()
    {
        var handler = CreateRenewalHandler();
        var (client, apiClient) = await CreateSignedInClient(handler);
        using (client)
        {
            await apiClient.RenewSessionAsync(ApiBaseUrl, "en", TestContext.Current.CancellationToken);
            await apiClient.GetResourceAsync(ApiBaseUrl, "/api/v1/admin/accounts", "en", TestContext.Current.CancellationToken);

            Assert.Equal(
                new RecordedRequest(
                    HttpMethod.Get,
                    new Uri("http://localhost:5000/api/v1/admin/accounts"),
                    "Bearer renewed-token",
                    null),
                handler.Requests[4]);
        }
    }

    [Fact]
    public async Task SignOutSendsTheCurrentSessionToken()
    {
        var handler = CreateRenewalHandler();
        var (client, apiClient) = await CreateSignedInClient(handler);
        using (client)
        {
            await apiClient.SignOutAsync(ApiBaseUrl, "en", TestContext.Current.CancellationToken);

            Assert.Equal(
                new RecordedRequest(
                    HttpMethod.Delete,
                    new Uri("http://localhost:5000/api/v2/auth/logout"),
                    "Bearer initial-token",
                    null),
                handler.Requests[3]);
        }
    }

    [Fact]
    public async Task SignOutAfterRenewalSendsTheRotatedSessionToken()
    {
        var handler = CreateRenewalHandler();
        var (client, apiClient) = await CreateSignedInClient(handler);
        using (client)
        {
            await apiClient.RenewSessionAsync(ApiBaseUrl, "en", TestContext.Current.CancellationToken);

            await apiClient.SignOutAsync(ApiBaseUrl, "en", TestContext.Current.CancellationToken);

            Assert.Equal(
                new RecordedRequest(
                    HttpMethod.Delete,
                    new Uri("http://localhost:5000/api/v2/auth/logout"),
                    "Bearer renewed-token",
                    null),
                handler.Requests[4]);
        }
    }

    [Fact]
    public async Task ProtectedRequestAfterSignOutHasNoSessionToken()
    {
        var handler = CreateRenewalHandler();
        var (client, apiClient) = await CreateSignedInClient(handler);
        using (client)
        {
            await apiClient.SignOutAsync(ApiBaseUrl, "en", TestContext.Current.CancellationToken);
            await apiClient.GetResourceAsync(ApiBaseUrl, "/api/v1/admin/accounts", "en", TestContext.Current.CancellationToken);

            Assert.Equal(
                new RecordedRequest(
                    HttpMethod.Get,
                    new Uri("http://localhost:5000/api/v1/admin/accounts"),
                    null,
                    null),
                handler.Requests[4]);
        }
    }

    [Fact]
    public async Task UnauthorizedProtectedRequestThrowsManagementApiException()
    {
        var handler = CreateUnauthorizedHandler();
        var (client, apiClient) = await CreateSignedInClient(handler);
        using (client)
        {
            var exception = await Record.ExceptionAsync(() =>
                apiClient.GetResourceAsync(ApiBaseUrl, "/api/v1/admin/accounts", "en", TestContext.Current.CancellationToken));

            Assert.Equal(
                new RequestFailureSnapshot(
                    typeof(ManagementApiException),
                    new RecordedRequest(
                        HttpMethod.Get,
                        new Uri("http://localhost:5000/api/v1/admin/accounts"),
                        "Bearer expired-token",
                        null)),
                new RequestFailureSnapshot(exception?.GetType(), handler.Requests[3]));
        }
    }

    [Fact]
    public async Task ProtectedRequestAfterUnauthorizedResponseHasNoSessionToken()
    {
        var handler = CreateUnauthorizedHandler();
        var (client, apiClient) = await CreateSignedInClient(handler);
        using (client)
        {
            try
            {
                await apiClient.GetResourceAsync(ApiBaseUrl, "/api/v1/admin/accounts", "en", TestContext.Current.CancellationToken);
            }
            catch (ManagementApiException)
            {
            }

            await apiClient.GetResourceAsync(ApiBaseUrl, "/api/v1/admin/accounts", "en", TestContext.Current.CancellationToken);

            Assert.Equal(
                new RecordedRequest(
                    HttpMethod.Get,
                    new Uri("http://localhost:5000/api/v1/admin/accounts"),
                    null,
                    null),
                handler.Requests[4]);
        }
    }

    [Fact]
    public async Task SignInRejectsRemoteCleartext()
    {
        var handler = CreateStandardHandler("token");
        using var client = new HttpClient(handler);
        var apiClient = new ManagementApiClient(client);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            apiClient.SignInAsync("http://remote.example/api", "operator", "password", "en", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SignInDoesNotSendCredentialsOverRemoteCleartext()
    {
        var handler = CreateStandardHandler("token");
        using var client = new HttpClient(handler);
        var apiClient = new ManagementApiClient(client);
        try
        {
            await apiClient.SignInAsync("http://remote.example/api", "operator", "password", "en", TestContext.Current.CancellationToken);
        }
        catch (InvalidOperationException)
        {
        }

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SessionTokenIsNotSentToAnotherApiOrigin()
    {
        var handler = CreateStandardHandler("original-token");
        var (client, apiClient) = await CreateSignedInClient(handler);
        using (client)
        {
            await apiClient.GetResourceAsync("https://other.example/api", "/api/v1/admin/accounts", "en", TestContext.Current.CancellationToken);

            Assert.Equal(
                new RecordedRequest(
                    HttpMethod.Get,
                    new Uri("https://other.example/api/v1/admin/accounts"),
                    null,
                    null),
                handler.Requests[3]);
        }
    }

    [Fact]
    public async Task RenewalRejectsAnotherApiOrigin()
    {
        var handler = CreateStandardHandler("original-token");
        var (client, apiClient) = await CreateSignedInClient(handler);
        using (client)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                apiClient.RenewSessionAsync("https://other.example/api", "en", TestContext.Current.CancellationToken));
        }
    }

    [Fact]
    public async Task SignOutUsesTheOriginalApiOriginWhenAnotherOriginIsRequested()
    {
        var handler = CreateRenewalHandler("original-token");
        var (client, apiClient) = await CreateSignedInClient(handler);
        using (client)
        {
            await apiClient.SignOutAsync("https://other.example/api", "en", TestContext.Current.CancellationToken);

            Assert.Equal(
                new RecordedRequest(
                    HttpMethod.Delete,
                    new Uri("http://localhost:5000/api/v2/auth/logout"),
                    "Bearer original-token",
                    null),
                handler.Requests[3]);
        }
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
            apiClient.SignInAsync(ApiBaseUrl, "operator", "local-password", "en", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SignInSendsNoCredentialsWhenAdvertisedActionIsCrossOrigin()
    {
        const string unsafeVersion =
            """{"class":["api","version"],"actions":[{"name":"administrator-sign-in","href":"https://other.example/login","method":"POST","type":"application/json"}]}""";
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(CreateSirenResponse(SessionResource, "token")),
            unsafeVersion);
        using var client = new HttpClient(handler);
        var apiClient = new ManagementApiClient(client);
        try
        {
            await apiClient.SignInAsync(ApiBaseUrl, "operator", "local-password", "en", TestContext.Current.CancellationToken);
        }
        catch (InvalidOperationException)
        {
        }

        Assert.Equal(
            "GET,GET",
            string.Join(',', handler.Requests.Select(request => request.Method.Method)));
    }

    private static RecordingHandler CreateStandardHandler(string token) =>
        new((request, _) => Task.FromResult(
            request.Method == HttpMethod.Post
                ? CreateSirenResponse(SessionResource, token)
                : CreateSirenResponse(Resource)));

    private static RecordingHandler CreateRenewalHandler(string initialToken = "initial-token") =>
        new((request, _) => Task.FromResult(
            request.Method switch
            {
                var method when method == HttpMethod.Post &&
                                request.RequestUri?.AbsolutePath.EndsWith("/refresh", StringComparison.Ordinal) == true =>
                    CreateSirenResponse(SessionResource, "renewed-token"),
                var method when method == HttpMethod.Post => CreateSirenResponse(SessionResource, initialToken),
                var method when method == HttpMethod.Delete => new HttpResponseMessage(HttpStatusCode.NoContent),
                _ => CreateSirenResponse(Resource)
            }));

    private static RecordingHandler CreateUnauthorizedHandler()
    {
        var resourceRequests = 0;
        return new RecordingHandler((request, _) =>
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
    }

    private static async Task<(HttpClient Client, ManagementApiClient ApiClient)> CreateSignedInClient(
        RecordingHandler handler)
    {
        var client = new HttpClient(handler);
        var apiClient = new ManagementApiClient(client);
        await apiClient.SignInAsync(ApiBaseUrl, "operator", "local-password", "en", TestContext.Current.CancellationToken);
        return (client, apiClient);
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

    private sealed record RequestFailureSnapshot(Type? ExceptionType, RecordedRequest Request);

    private sealed record RecordedRequest(HttpMethod Method, Uri? Uri, string? Authorization, string? Body);
}
