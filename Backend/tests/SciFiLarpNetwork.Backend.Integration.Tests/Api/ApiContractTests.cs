using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using SciFiLarpNetwork.Backend.Tests.Integration.Api.Fixtures;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Api;

[Collection("Backend database integration")]
public sealed class ApiContractTests(ApiApplicationFactory factory) : IClassFixture<ApiApplicationFactory>
{
    [Fact]
    public async Task ApiRootReturnsLocalizedSirenDiscoveryLinksWithPathBase()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/local/api", TestContext.Current.CancellationToken);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(
            new
            {
                Status = HttpStatusCode.OK,
                ContentType = "application/vnd.siren+json",
                Class = "api",
                Name = "Sci-Fi LARP Network",
                Links = "self=/local/api|service-desc=/local/openapi/v1.json|urn:scifilarnetwork:rels:latest-version=/local/api/v1",
                HasActions = false
            },
            new
            {
                Status = response.StatusCode,
                ContentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty,
                Class = body.RootElement.GetProperty("class")[0].GetString() ?? string.Empty,
                Name = body.RootElement.GetProperty("properties").GetProperty("name").GetString() ?? string.Empty,
                Links = LinkSnapshot(body.RootElement),
                HasActions = body.RootElement.TryGetProperty("actions", out _)
            });
    }

    [Fact]
    public async Task VersionedApiRootUsesRequestedItalianLanguage()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/local/api/v1");
        request.Headers.AcceptLanguage.ParseAdd("it-IT");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken), cancellationToken: TestContext.Current.CancellationToken);

        var serviceDescriptionTitle = body.RootElement.GetProperty("links").EnumerateArray()
            .Where(link => HasRelation(link, "service-desc"))
            .Select(link => link.GetProperty("title").GetString())
            .SingleOrDefault();
        Assert.Equal(
            new { Status = HttpStatusCode.OK, Language = "it", Name = "Sci-Fi LARP Network", ServiceDescriptionTitle = "Descrizione API" },
            new
            {
                Status = response.StatusCode,
                Language = response.Content.Headers.ContentLanguage?.FirstOrDefault() ?? string.Empty,
                Name = body.RootElement.GetProperty("properties").GetProperty("name").GetString() ?? string.Empty,
                ServiceDescriptionTitle = serviceDescriptionTitle ?? string.Empty
            });
    }

    [Fact]
    public async Task OpenApiDocumentIsAvailableAsJson()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/local/openapi/v1.json", TestContext.Current.CancellationToken);

        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(
            new { Status = HttpStatusCode.OK, ContentType = "application/json", HasOpenApi = true, HasApiPath = true, ServerUrl = "/local" },
            new
            {
                Status = response.StatusCode,
                ContentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty,
                HasOpenApi = body.RootElement.TryGetProperty("openapi", out _),
                HasApiPath = body.RootElement.GetProperty("paths").EnumerateObject()
                    .Any(path => path.Name.Contains("/api", StringComparison.Ordinal)),
                ServerUrl = body.RootElement.GetProperty("servers")[0].GetProperty("url").GetString() ?? string.Empty
            });
    }

    [Fact]
    public async Task ReadinessEndpointReportsDatabaseAndProjectionDiagnosticsWithoutSecrets()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/local/health/ready", TestContext.Current.CancellationToken);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(
            new { Status = HttpStatusCode.OK, Overall = "Healthy", EventStore = "Healthy", ExposesPassword = false },
            new
            {
                Status = response.StatusCode,
                Overall = body.RootElement.GetProperty("status").GetString() ?? string.Empty,
                EventStore = body.RootElement.GetProperty("checks").GetProperty("event-store").GetProperty("status").GetString() ?? string.Empty,
                ExposesPassword = body.RootElement.GetRawText().Contains("Password=", StringComparison.OrdinalIgnoreCase)
            });
    }

    [Fact]
    public async Task SirenActionAdvertisesTypedJsonRequestFields()
    {
        using var client = factory.CreateClient();
        using var formResponse = await client.GetAsync("/local/api/v1/contract-test/message-action", TestContext.Current.CancellationToken);
        using var formBody = await JsonDocument.ParseAsync(await formResponse.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken), cancellationToken: TestContext.Current.CancellationToken);
        var action = formBody.RootElement.GetProperty("actions")[0];

        Assert.Equal(
            new { Status = HttpStatusCode.OK, Name = "submit-message", Method = "POST", Type = "application/json", FieldName = "name" },
            new
            {
                Status = formResponse.StatusCode,
                Name = action.GetProperty("name").GetString() ?? string.Empty,
                Method = action.GetProperty("method").GetString() ?? string.Empty,
                Type = action.GetProperty("type").GetString() ?? string.Empty,
                FieldName = action.GetProperty("fields")[0].GetProperty("name").GetString() ?? string.Empty
            });
    }

    [Fact]
    public async Task AdvertisedJsonActionAcceptsTypedRequestProperties()
    {
        using var client = factory.CreateClient();
        using var formResponse = await client.GetAsync("/local/api/v1/contract-test/message-action", TestContext.Current.CancellationToken);
        using var formBody = await JsonDocument.ParseAsync(await formResponse.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken), cancellationToken: TestContext.Current.CancellationToken);
        var actionHref = formBody.RootElement.GetProperty("actions")[0].GetProperty("href").GetString();

        using var submitResponse = await client.PostAsync(
            actionHref,
            new StringContent("{\"name\":\"test message\"}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);
        using var submitBody = await JsonDocument.ParseAsync(await submitResponse.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(
            new { Status = HttpStatusCode.OK, Name = "test message" },
            new
            {
                Status = submitResponse.StatusCode,
                Name = submitBody.RootElement.GetProperty("properties").GetProperty("name").GetString() ?? string.Empty
            });
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

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken), cancellationToken: TestContext.Current.CancellationToken);
        var properties = body.RootElement.GetProperty("properties");

        Assert.Equal(
            new
            {
                Status = HttpStatusCode.BadRequest,
                ContentType = "application/vnd.siren+json",
                Code = "errors.validation",
                Detail = "La richiesta contiene valori non validi.",
                NameError = "Inserisci un valore valido.",
                HasSelfLink = true
            },
            new
            {
                Status = response.StatusCode,
                ContentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty,
                Code = properties.GetProperty("code").GetString() ?? string.Empty,
                Detail = properties.GetProperty("detail").GetString() ?? string.Empty,
                NameError = properties.GetProperty("errors").GetProperty("Name")[0].GetString() ?? string.Empty,
                HasSelfLink = body.RootElement.GetProperty("links").EnumerateArray().Any(link => HasRelation(link, "self"))
            });
    }

    [Fact]
    public async Task MissingRouteReturnsSirenNotFoundError()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/local/api/v1/not-implemented", TestContext.Current.CancellationToken);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(
            new { Status = HttpStatusCode.NotFound, ContentType = "application/vnd.siren+json", Code = "errors.notFound" },
            new
            {
                Status = response.StatusCode,
                ContentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty,
                Code = body.RootElement.GetProperty("properties").GetProperty("code").GetString() ?? string.Empty
            });
    }

    [Fact]
    public async Task ApiExceptionReturnsStableConflictCode()
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsync("/local/api/v1/contract-test/conflict", null, TestContext.Current.CancellationToken);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(
            new { Status = HttpStatusCode.Conflict, ContentType = "application/vnd.siren+json", Code = "errors.conflict" },
            new
            {
                Status = response.StatusCode,
                ContentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty,
                Code = body.RootElement.GetProperty("properties").GetProperty("code").GetString() ?? string.Empty
            });
    }

    [Fact]
    public async Task UnexpectedExceptionIsLoggedAndDetailsAreNotDisclosed()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/local/api/v1/contract-test/unexpected-error", TestContext.Current.CancellationToken);
        var bodyText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            new { Status = HttpStatusCode.InternalServerError, ContainsGenericError = true, LeaksException = false },
            new
            {
                Status = response.StatusCode,
                ContainsGenericError = bodyText.Contains("errors.internalServerError", StringComparison.Ordinal),
                LeaksException = bodyText.Contains("This message must not reach the client.", StringComparison.Ordinal)
            });
    }

    [Fact]
    public async Task ProtectedActionRejectsAnonymousRequest()
    {
        using var client = factory.CreateClient();
        using var anonymousResponse = await client.PostAsync("/local/api/v1/contract-test/protected-action", null, TestContext.Current.CancellationToken);

        Assert.Equal(
            new { Status = HttpStatusCode.Unauthorized, ContentType = "application/vnd.siren+json" },
            new
            {
                Status = anonymousResponse.StatusCode,
                ContentType = anonymousResponse.Content.Headers.ContentType?.MediaType ?? string.Empty
            });
    }

    [Fact]
    public async Task ProtectedActionAcceptsAuthenticatedPrincipal()
    {
        using var client = factory.CreateClient();
        using var authenticatedRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/local/api/v1/contract-test/protected-action");
        authenticatedRequest.Headers.Add("X-Test-Authenticated", "true");
        using var authenticatedResponse = await client.SendAsync(authenticatedRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, authenticatedResponse.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedPrincipalWithoutRequiredPermissionGetsSirenForbiddenError()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/local/api/v1/contract-test/admin-action");
        request.Headers.Add("X-Test-Authenticated", "true");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(
            new { Status = HttpStatusCode.Forbidden, ContentType = "application/vnd.siren+json", Code = "errors.forbidden" },
            new
            {
                Status = response.StatusCode,
                ContentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty,
                Code = body.RootElement.GetProperty("properties").GetProperty("code").GetString() ?? string.Empty
            });
    }

    [Fact]
    public void InvalidPathBasePreventsStartup()
    {
        using var invalidFactory = factory.WithWebHostBuilder(builder => builder.UseSetting("PathBase", "local/"));

        var exception = Record.Exception(() => invalidFactory.CreateClient());

        Assert.Equal(
            "PathBase must be an absolute path without a trailing slash, query, or fragment.",
            exception?.GetBaseException().Message);
    }

    private static string LinkSnapshot(JsonElement entity) =>
        string.Join('|', entity.GetProperty("links").EnumerateArray()
            .Select(link => $"{string.Join(',', link.GetProperty("rel").EnumerateArray().Select(rel => rel.GetString()))}={link.GetProperty("href").GetString()}")
            .OrderBy(value => value, StringComparer.Ordinal));

    private static bool HasRelation(JsonElement link, string relation) =>
        link.GetProperty("rel").EnumerateArray().Any(value => value.GetString() == relation);
}
