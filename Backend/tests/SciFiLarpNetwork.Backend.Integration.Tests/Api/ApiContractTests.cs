using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SciFiLarpNetwork.Backend.Tests.Integration.Api.Fixtures;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Api;

public sealed class ApiContractTests(ApiApplicationFactory factory) : IClassFixture<ApiApplicationFactory>
{
    [Fact]
    public async Task ApiRootReturnsLocalizedSirenDiscoveryLinksWithPathBase()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/local/api");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/vnd.siren+json", response.Content.Headers.ContentType?.MediaType);

        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("api", body.RootElement.GetProperty("class")[0].GetString());
        Assert.Equal("Sci-Fi LARP Network", body.RootElement.GetProperty("properties").GetProperty("name").GetString());

        var links = body.RootElement.GetProperty("links").EnumerateArray().ToArray();
        Assert.Contains(links, link => HasRelation(link, "self") && link.GetProperty("href").GetString() == "/local/api");
        Assert.Contains(links, link => HasRelation(link, "urn:scifilarnetwork:rels:latest-version") &&
            link.GetProperty("href").GetString() == "/local/api/v1");
        Assert.Contains(links, link => HasRelation(link, "service-desc") &&
            link.GetProperty("href").GetString() == "/local/openapi/v1.json");
        Assert.False(body.RootElement.TryGetProperty("actions", out _));
    }

    [Fact]
    public async Task VersionedApiRootUsesRequestedItalianLanguage()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/local/api/v1");
        request.Headers.AcceptLanguage.ParseAdd("it-IT");

        using var response = await client.SendAsync(request);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("it", response.Content.Headers.ContentLanguage?.FirstOrDefault());
        Assert.Equal("Sci-Fi LARP Network", body.RootElement.GetProperty("properties").GetProperty("name").GetString());
        Assert.Contains(
            body.RootElement.GetProperty("links").EnumerateArray(),
            link => HasRelation(link, "service-desc") &&
                link.GetProperty("title").GetString() == "Descrizione API");
    }

    [Fact]
    public async Task OpenApiDocumentIsAvailableAsJson()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/local/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(body.RootElement.TryGetProperty("openapi", out _));
        Assert.Contains("/api", body.RootElement.GetProperty("paths").EnumerateObject().Select(path => path.Name));
        Assert.Equal("/local", body.RootElement.GetProperty("servers")[0].GetProperty("url").GetString());
    }

    [Fact]
    public async Task ReadinessEndpointReportsDatabaseAndProjectionDiagnosticsWithoutSecrets()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/local/health/ready");
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
        Assert.Equal(
            "Healthy",
            body.RootElement.GetProperty("checks").GetProperty("event-store").GetProperty("status").GetString());
        Assert.DoesNotContain("Password=", body.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SirenActionFieldsMapToTypedJsonRequestProperties()
    {
        using var client = factory.CreateClient();
        using var formResponse = await client.GetAsync("/local/api/v1/contract-test/message-action");
        using var formBody = await JsonDocument.ParseAsync(await formResponse.Content.ReadAsStreamAsync());
        var action = formBody.RootElement.GetProperty("actions")[0];

        Assert.Equal("submit-message", action.GetProperty("name").GetString());
        Assert.Equal("POST", action.GetProperty("method").GetString());
        Assert.Equal("application/json", action.GetProperty("type").GetString());
        Assert.Equal("name", action.GetProperty("fields")[0].GetProperty("name").GetString());

        using var submitResponse = await client.PostAsync(
            action.GetProperty("href").GetString(),
            new StringContent("{\"name\":\"test message\"}", Encoding.UTF8, "application/json"));
        using var submitBody = await JsonDocument.ParseAsync(await submitResponse.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
        Assert.Equal(
            "test message",
            submitBody.RootElement.GetProperty("properties").GetProperty("name").GetString());
    }

    [Fact]
    public async Task InvalidRequestReturnsFieldLevelSirenErrorInRequestedLanguage()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/local/api/v1/contract-test/validation")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.AcceptLanguage.ParseAdd("it");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.siren+json"));

        using var response = await client.SendAsync(request);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var properties = body.RootElement.GetProperty("properties");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/vnd.siren+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("errors.validation", properties.GetProperty("code").GetString());
        Assert.Equal("La richiesta contiene valori non validi.", properties.GetProperty("detail").GetString());
        Assert.Equal("Inserisci un valore valido.", properties.GetProperty("errors").GetProperty("Name")[0].GetString());
        Assert.Contains(
            body.RootElement.GetProperty("links").EnumerateArray(),
            link => HasRelation(link, "self"));
    }

    [Fact]
    public async Task MissingRouteReturnsSirenNotFoundError()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/local/api/v1/not-implemented");
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/vnd.siren+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("errors.notFound", body.RootElement.GetProperty("properties").GetProperty("code").GetString());
    }

    [Fact]
    public async Task ApiExceptionReturnsStableConflictCode()
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsync("/local/api/v1/contract-test/conflict", null);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/vnd.siren+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("errors.conflict", body.RootElement.GetProperty("properties").GetProperty("code").GetString());
    }

    [Fact]
    public async Task UnexpectedExceptionIsLoggedAndDetailsAreNotDisclosed()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/local/api/v1/contract-test/unexpected-error");
        var bodyText = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("errors.internalServerError", bodyText, StringComparison.Ordinal);
        Assert.DoesNotContain("This message must not reach the client.", bodyText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProtectedActionRejectsAnonymousRequestAndAcceptsAuthenticatedPrincipal()
    {
        using var client = factory.CreateClient();
        using var anonymousResponse = await client.PostAsync("/local/api/v1/contract-test/protected-action", null);
        using var authenticatedRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/local/api/v1/contract-test/protected-action");
        authenticatedRequest.Headers.Add("X-Test-Authenticated", "true");
        using var authenticatedResponse = await client.SendAsync(authenticatedRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal("application/vnd.siren+json", anonymousResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.OK, authenticatedResponse.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedPrincipalWithoutRequiredPermissionGetsSirenForbiddenError()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/local/api/v1/contract-test/admin-action");
        request.Headers.Add("X-Test-Authenticated", "true");

        using var response = await client.SendAsync(request);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/vnd.siren+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("errors.forbidden", body.RootElement.GetProperty("properties").GetProperty("code").GetString());
    }

    private static bool HasRelation(JsonElement link, string relation) =>
        link.GetProperty("rel").EnumerateArray().Any(value => value.GetString() == relation);
}
