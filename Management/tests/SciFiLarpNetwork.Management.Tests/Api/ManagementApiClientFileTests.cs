using System.Net;
using System.Net.Http.Headers;
using System.Text;
using SciFiLarpNetwork.Management.Api;

namespace SciFiLarpNetwork.Management.Tests.Api;

public sealed class ManagementApiClientFileTests : IDisposable
{
    private const string TemplateHref = "/api/v1/card-templates/character/source";
    private static readonly byte[] TemplateBytes = "#let data = json(\"cards.json\")"u8.ToArray();
    private HttpClient? _httpClient;

    [Fact]
    public async Task FileDownloadReturnsTheBytes()
    {
        var client = CreateClient(_ => FileResponse("text/plain"));

        Assert.Equal(TemplateBytes, await GetAsync(client, TemplateHref));
    }

    [Fact]
    public async Task FileDownloadAcceptsTheFileTypeAndSirenErrors()
    {
        HttpRequestMessage? sent = null;
        var client = CreateClient(request =>
        {
            sent = request;
            return FileResponse("text/plain");
        });

        await GetAsync(client, TemplateHref);

        Assert.Equal("text/plain,application/vnd.siren+json", string.Join(',', sent!.Headers.Accept.Select(header => header.MediaType)));
    }

    [Fact]
    public async Task FileDownloadRejectsAnotherContentType()
    {
        var client = CreateClient(_ => FileResponse("text/html"));

        var failure = await Record.ExceptionAsync(() => GetAsync(client, TemplateHref));

        Assert.IsType<InvalidOperationException>(failure);
    }

    [Fact]
    public async Task FileDownloadRefusesLinksOutsideTheApi()
    {
        var client = CreateClient(_ => FileResponse("text/plain"));

        var failure = await Record.ExceptionAsync(() => GetAsync(client, "http://elsewhere.example/template.typ"));

        Assert.IsType<InvalidOperationException>(failure);
    }

    [Fact]
    public async Task FileDownloadReportsApiErrorCodes()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("""{"class":["error"],"properties":{"code":"errors.forbidden"}}""", Encoding.UTF8,
                "application/vnd.siren+json")
        });

        var failure = await Assert.ThrowsAsync<ManagementApiException>(() => GetAsync(client, TemplateHref));

        Assert.Equal(("errors.forbidden", HttpStatusCode.Forbidden), (failure.ErrorCode, failure.StatusCode));
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    private static Task<byte[]> GetAsync(ManagementApiClient client, string href) =>
        client.GetFileAsync("http://localhost/api", href, "text/plain", "en", TestContext.Current.CancellationToken);

    private static HttpResponseMessage FileResponse(string mediaType)
    {
        var content = new ByteArrayContent(TemplateBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(mediaType) { CharSet = "utf-8" };
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    private ManagementApiClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        _httpClient = new HttpClient(new StubHandler(responseFactory));
        return new ManagementApiClient(_httpClient);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
