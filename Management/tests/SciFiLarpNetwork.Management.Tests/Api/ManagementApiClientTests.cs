using System.Net;
using System.Text;
using System.Text.Json;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Common.ApiLinkPolicy;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Management.Tests.Api;

public sealed class ManagementApiClientTests : IDisposable
{
    private static readonly JsonSerializerOptions FixtureJsonOptions =
        new(JsonSerializerDefaults.Web);
    private HttpClient? _httpClient;

    [Fact]
    public async Task DiscoverAsyncFollowsLatestVersionAndFiltersUnsafeNavigation()
    {
        var requests = new List<HttpRequestMessage>();
        var client = CreateClient(request =>
        {
            requests.Add(request);
            return request.RequestUri!.AbsolutePath switch
            {
                "/api/" => SirenResponse(
                    """{"class":["api"],"links":[{"rel":["urn:scifilarnetwork:rels:latest-version"],"href":"/api/v1"}]}"""),
                "/api/v1" => SirenResponse(
                    """{"class":["api-version"],"links":[{"rel":["urn:scifilarnetwork:rels:navigation"],"href":"/api/v1/messages","title":"Messages"},{"rel":["urn:scifilarnetwork:rels:navigation"],"href":"https://outside.invalid/","title":"External"},{"rel":null,"href":"/api/v1/invalid","title":"Malformed"}]}"""),
                _ => throw new InvalidOperationException("Unexpected API request.")
            };
        });

        var discovery = await client.DiscoverAsync("http://localhost:5000/api", "it", TestContext.Current.CancellationToken);

        Assert.Equal(
            new
            {
                ApiClass = "api-version",
                Navigation = "Messages|/api/v1/messages",
                RequestCount = 2,
                AcceptedMediaTypes = "application/vnd.siren+json,application/vnd.siren+json",
                AcceptedLanguages = "it,it"
            },
            new
            {
                ApiClass = discovery.ApiResource.Class![0],
                Navigation = string.Join('|', discovery.NavigationLinks.Select(link => $"{link.Title}|{link.Href}")),
                RequestCount = requests.Count,
                AcceptedMediaTypes = string.Join(',', requests.Select(request =>
                    string.Join(',', request.Headers.Accept.Select(header => header.MediaType)))),
                AcceptedLanguages = string.Join(',', requests.Select(request =>
                    string.Join(',', request.Headers.AcceptLanguage.Select(header => header.Value))))
            });
    }

    [Fact]
    public async Task DiscoverAsyncUsesRootWhenLatestVersionLinkIsUnsafe()
    {
        var requestCount = 0;
        var client = CreateClient(_ =>
        {
            requestCount++;
            return SirenResponse(
                """{"class":["api"],"links":[{"rel":["urn:scifilarnetwork:rels:latest-version"],"href":"https://outside.invalid/api/v1"}]}""");
        });

        var discovery = await client.DiscoverAsync("http://localhost/api", "en", TestContext.Current.CancellationToken);

        Assert.Equal(
            new { ApiClass = "api", NavigationCount = 0, RequestCount = 1 },
            new
            {
                ApiClass = discovery.ApiResource.Class![0],
                NavigationCount = discovery.NavigationLinks.Count,
                RequestCount = requestCount
            });
    }

    [Theory]
    [InlineData("localhost/api")]
    [InlineData("http://localhost/admin")]
    [InlineData("ftp://localhost/api")]
    [InlineData("http://user@localhost/api")]
    [InlineData("http://localhost/api?key=value")]
    public async Task DiscoverAsyncRejectsInvalidApiBaseUrls(string apiBaseUrl)
    {
        var client = CreateClient(_ => throw new InvalidOperationException("No request expected."));

        await Assert.ThrowsAsync<UriFormatException>(() =>
            client.DiscoverAsync(apiBaseUrl, "en", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetResourceAsyncRejectsTargetsOutsideConfiguredApiPath()
    {
        var client = CreateClient(_ => throw new InvalidOperationException("No request expected."));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetResourceAsync("http://localhost/api", "/api-other/messages", "en", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DiscoverAsyncUsesStableErrorCodeFromSirenError()
    {
        var client = CreateClient(_ =>
            SirenResponse(
                """{"class":["error"],"properties":{"code":"errors.forbidden"}}""",
                HttpStatusCode.Forbidden));

        var exception = await Record.ExceptionAsync(() =>
            client.DiscoverAsync("http://localhost/api", "en", TestContext.Current.CancellationToken));

        Assert.Equal(
            new ManagementApiExceptionSnapshot("errors.forbidden", HttpStatusCode.Forbidden),
            exception is ManagementApiException apiException
                ? new ManagementApiExceptionSnapshot(apiException.ErrorCode, apiException.StatusCode)
                : null);
    }

    [Fact]
    public async Task GetResourceAsyncRejectsSuccessfulNonJsonResponse()
    {
        var client = CreateClient(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not json", Encoding.UTF8, "text/plain")
            });

        await Assert.ThrowsAsync<JsonException>(() =>
            client.GetResourceAsync("http://localhost/api", "/api/v1", "en", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetResourceAsyncRejectsNonStandardJsonMediaTypes()
    {
        var client = CreateClient(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/notjson")
            });

        await Assert.ThrowsAsync<JsonException>(() =>
            client.GetResourceAsync("http://localhost/api", "/api/v1", "en", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SubmitActionAsyncSendsJsonAndReturnsSirenResource()
    {
        HttpRequestMessage? sentRequest = null;
        string? sentBody = null;
        var client = CreateClient(request =>
        {
            sentRequest = request;
            sentBody = request.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken).GetAwaiter().GetResult();
            return SirenResponse("""{"class":["message"]}""");
        });
        using var body = JsonDocument.Parse("""{"message":"hello"}""");
        var action = new SirenAction
        {
            Name = "send-message",
            Href = "/api/v1/messages",
            Method = "POST",
            Type = "application/json",
            Title = "Send message",
            Fields = []
        };

        var result = await client.SubmitActionAsync(
            "http://localhost/api",
            action,
            body.RootElement,
            "it",
            TestContext.Current.CancellationToken);

        Assert.Equal(
            new { ResourceClass = "message", Method = "POST", Language = "it", Body = """{"message":"hello"}""" },
            new
            {
                ResourceClass = result.Class![0],
                Method = sentRequest?.Method.Method ?? string.Empty,
                Language = sentRequest?.Headers.AcceptLanguage.SingleOrDefault()?.Value ?? string.Empty,
                Body = sentBody ?? string.Empty
            });
    }

    [Theory]
    [InlineData("method")]
    [InlineData("payload-type")]
    [InlineData("href")]
    public async Task SubmitActionAsyncRejectsUnsupportedMethodsAndPayloadTypes(string invalidPart)
    {
        var client = CreateClient(_ => throw new InvalidOperationException("No request expected."));
        using var body = JsonDocument.Parse("""{"value":1}""");
        var action = invalidPart switch
        {
            "method" => new SirenAction { Name = "submit", Href = "/api/v1", Method = "TRACE" },
            "payload-type" => new SirenAction
            {
                Name = "submit",
                Href = "/api/v1",
                Method = "POST",
                Type = "text/plain"
            },
            "href" => new SirenAction { Name = "submit", Href = "", Method = "POST" },
            _ => throw new ArgumentOutOfRangeException(nameof(invalidPart))
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.SubmitActionAsync(
                "http://localhost/api",
                action,
                invalidPart == "payload-type" ? body.RootElement : null,
                "en",
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SubmitActionAsyncRejectsBodyForGetAction()
    {
        var client = CreateClient(_ => throw new InvalidOperationException("No request expected."));
        using var body = JsonDocument.Parse("""{"value":1}""");
        var action = new SirenAction
        {
            Name = "lookup",
            Href = "/api/v1",
            Method = "GET",
            Type = "application/json"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.SubmitActionAsync("http://localhost/api", action, body.RootElement, "en", TestContext.Current.CancellationToken));
    }

    [Fact]
    public void SirenFieldStoresDeclaredFieldMetadata()
    {
        using var value = JsonDocument.Parse("\"Hello\"");
        var field = new SirenField
        {
            Name = "message",
            Type = "text",
            Value = value.RootElement,
            Title = "Message"
        };

        Assert.Equal(
            new { Name = "message", Type = "text", Value = "Hello", Title = "Message" },
            new { field.Name, field.Type, Value = field.Value?.GetString() ?? string.Empty, field.Title });
    }

    [Theory]
    [MemberData(nameof(ApiLinkPolicyCases))]
    public void ApiLinkPolicyMatchesSharedConformanceCases(
        string name,
        string apiBaseUrl,
        string href,
        string? expectedHref)
    {
        _ = name;
        var apiRoot = ApiLinkPolicy.CreateApiRoot(apiBaseUrl);
        var resolved = ApiLinkPolicy.TryResolve(apiRoot, href, out var target);

        Assert.Equal(expectedHref, resolved ? target.AbsoluteUri : null);
    }

    public static IEnumerable<object[]> ApiLinkPolicyCases
    {
        get
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "test-cases.json");
            var testCases = JsonSerializer.Deserialize<ApiLinkPolicyTestCase[]>(
                File.ReadAllText(fixturePath),
                FixtureJsonOptions)!;

            return testCases.Select(testCase => new object[]
            {
                testCase.Name,
                testCase.ApiBaseUrl,
                testCase.Href,
                testCase.ExpectedHref!
            });
        }
    }

    private sealed record ManagementApiExceptionSnapshot(string ErrorCode, HttpStatusCode StatusCode);

    private sealed record ApiLinkPolicyTestCase(
        string Name,
        string ApiBaseUrl,
        string Href,
        string? ExpectedHref);

    private ManagementApiClient CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        _httpClient = new HttpClient(new StubHandler(responseFactory));
        return new ManagementApiClient(_httpClient);
    }

    public void Dispose() => _httpClient?.Dispose();

    private static HttpResponseMessage SirenResponse(
        string body,
        HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/vnd.siren+json")
        };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
