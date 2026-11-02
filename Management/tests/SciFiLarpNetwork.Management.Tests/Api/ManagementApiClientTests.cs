using System.Net;
using System.Text;
using System.Text.Json;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Common.ApiLinkPolicy;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Management.Tests.Api;

public sealed class ManagementApiClientTests
{
    private static readonly JsonSerializerOptions FixtureJsonOptions =
        new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task DiscoverAsyncFollowsLatestVersionAndFiltersUnsafeNavigation()
    {
        var requests = new List<HttpRequestMessage>();
        using var client = CreateClient(request =>
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

        var discovery = await client.DiscoverAsync("http://localhost:5000/api", "it");

        Assert.Equal("api-version", discovery.ApiResource.Class![0]);
        var link = Assert.Single(discovery.NavigationLinks);
        Assert.Equal("Messages", link.Title);
        Assert.Equal(2, requests.Count);
        Assert.All(requests, request =>
        {
            Assert.Contains(request.Headers.Accept, header =>
                header.MediaType == "application/vnd.siren+json");
            Assert.Equal("it", Assert.Single(request.Headers.AcceptLanguage).Value);
        });
    }

    [Fact]
    public async Task DiscoverAsyncUsesRootWhenLatestVersionLinkIsUnsafe()
    {
        var requestCount = 0;
        using var client = CreateClient(_ =>
        {
            requestCount++;
            return SirenResponse(
                """{"class":["api"],"links":[{"rel":["urn:scifilarnetwork:rels:latest-version"],"href":"https://outside.invalid/api/v1"}]}""");
        });

        var discovery = await client.DiscoverAsync("http://localhost/api", "en");

        Assert.Equal("api", discovery.ApiResource.Class![0]);
        Assert.Empty(discovery.NavigationLinks);
        Assert.Equal(1, requestCount);
    }

    [Theory]
    [InlineData("localhost/api")]
    [InlineData("http://localhost/admin")]
    [InlineData("ftp://localhost/api")]
    [InlineData("http://user@localhost/api")]
    [InlineData("http://localhost/api?key=value")]
    public async Task DiscoverAsyncRejectsInvalidApiBaseUrls(string apiBaseUrl)
    {
        using var client = CreateClient(_ => throw new InvalidOperationException("No request expected."));

        await Assert.ThrowsAsync<UriFormatException>(() =>
            client.DiscoverAsync(apiBaseUrl, "en"));
    }

    [Fact]
    public async Task GetResourceAsyncRejectsTargetsOutsideConfiguredApiPath()
    {
        using var client = CreateClient(_ => throw new InvalidOperationException("No request expected."));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetResourceAsync("http://localhost/api", "/api-other/messages", "en"));
    }

    [Fact]
    public async Task DiscoverAsyncUsesStableErrorCodeFromSirenError()
    {
        using var client = CreateClient(_ =>
            SirenResponse(
                """{"class":["error"],"properties":{"code":"errors.forbidden"}}""",
                HttpStatusCode.Forbidden));

        var exception = await Assert.ThrowsAsync<ManagementApiException>(() =>
            client.DiscoverAsync("http://localhost/api", "en"));

        Assert.Equal("errors.forbidden", exception.ErrorCode);
        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
    }

    [Fact]
    public async Task GetResourceAsyncRejectsSuccessfulNonJsonResponse()
    {
        using var client = CreateClient(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not json", Encoding.UTF8, "text/plain")
            });

        await Assert.ThrowsAsync<JsonException>(() =>
            client.GetResourceAsync("http://localhost/api", "/api/v1", "en"));
    }

    [Fact]
    public async Task GetResourceAsyncRejectsNonStandardJsonMediaTypes()
    {
        using var client = CreateClient(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/notjson")
            });

        await Assert.ThrowsAsync<JsonException>(() =>
            client.GetResourceAsync("http://localhost/api", "/api/v1", "en"));
    }

    [Fact]
    public async Task SubmitActionAsyncSendsJsonAndReturnsSirenResource()
    {
        HttpRequestMessage? sentRequest = null;
        string? sentBody = null;
        using var client = CreateClient(request =>
        {
            sentRequest = request;
            sentBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
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
            "it");

        Assert.Equal("message", result.Class![0]);
        Assert.NotNull(sentRequest);
        Assert.Equal(HttpMethod.Post, sentRequest.Method);
        Assert.Equal("it", Assert.Single(sentRequest.Headers.AcceptLanguage).Value);
        Assert.Equal("""{"message":"hello"}""", sentBody);
    }

    [Fact]
    public async Task SubmitActionAsyncRejectsUnsupportedMethodsAndPayloadTypes()
    {
        using var client = CreateClient(_ => throw new InvalidOperationException("No request expected."));
        using var body = JsonDocument.Parse("""{"value":1}""");
        var unsupportedMethod = new SirenAction { Name = "submit", Href = "/api/v1", Method = "TRACE" };
        var unsupportedType = new SirenAction
        {
            Name = "submit",
            Href = "/api/v1",
            Method = "POST",
            Type = "text/plain"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.SubmitActionAsync("http://localhost/api", unsupportedMethod, null, "en"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.SubmitActionAsync("http://localhost/api", unsupportedType, body.RootElement, "en"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.SubmitActionAsync(
                "http://localhost/api",
                new SirenAction { Name = "submit", Href = "", Method = "POST" },
                null,
                "en"));
    }

    [Fact]
    public async Task SubmitActionAsyncRejectsBodyForGetAction()
    {
        using var client = CreateClient(_ => throw new InvalidOperationException("No request expected."));
        using var body = JsonDocument.Parse("""{"value":1}""");
        var action = new SirenAction
        {
            Name = "lookup",
            Href = "/api/v1",
            Method = "GET",
            Type = "application/json"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.SubmitActionAsync("http://localhost/api", action, body.RootElement, "en"));
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

        Assert.Equal("message", field.Name);
        Assert.Equal("text", field.Type);
        Assert.Equal("Hello", field.Value?.GetString());
        Assert.Equal("Message", field.Title);
    }

    [Fact]
    public void ApiLinkPolicyMatchesSharedConformanceCases()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "test-cases.json");
        var testCases = JsonSerializer.Deserialize<ApiLinkPolicyTestCase[]>(
            File.ReadAllText(fixturePath),
            FixtureJsonOptions)!;

        foreach (var testCase in testCases)
        {
            var apiRoot = ApiLinkPolicy.CreateApiRoot(testCase.ApiBaseUrl);
            var resolved = ApiLinkPolicy.TryResolve(apiRoot, testCase.Href, out var target);

            Assert.Equal(testCase.ExpectedHref is not null, resolved);
            Assert.Equal(testCase.ExpectedHref, resolved ? target.AbsoluteUri : null);
        }
    }

    private sealed record ApiLinkPolicyTestCase(
        string Name,
        string ApiBaseUrl,
        string Href,
        string? ExpectedHref);

    private static ManagementApiClient CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) =>
        new(new HttpClient(new StubHandler(responseFactory)));

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
