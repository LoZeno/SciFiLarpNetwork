using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;

namespace SciFiLarpNetwork.Management.Tests.Api;

public sealed class ManagementApiClientDownloadTests : IDisposable
{
    private const string PdfMediaType = "application/pdf";
    private static readonly byte[] PdfBytes = [0x25, 0x50, 0x44, 0x46, 0x2D];
    private static readonly SirenAction PrintAction = new()
    {
        Name = "print-character-cards",
        Href = "/api/v1/character-cards",
        Method = "POST",
        Type = "application/json"
    };

    private static readonly JsonElement Body = JsonSerializer.SerializeToElement(new { characterIds = Array.Empty<Guid>() });
    private HttpClient? _httpClient;

    [Fact]
    public async Task DownloadReturnsTheFileBytes()
    {
        var client = CreateClient(_ => FileResponse(PdfMediaType));

        var content = await DownloadAsync(client);

        Assert.Equal(PdfBytes, content);
    }

    [Fact]
    public async Task DownloadAcceptsTheFileTypeAndSirenErrors()
    {
        HttpRequestMessage? sent = null;
        var client = CreateClient(request =>
        {
            sent = request;
            return FileResponse(PdfMediaType);
        });

        await DownloadAsync(client);

        Assert.Equal("application/pdf,application/vnd.siren+json", string.Join(',', sent!.Headers.Accept.Select(header => header.MediaType)));
    }

    [Fact]
    public async Task DownloadSendsTheJsonBody()
    {
        string? sentBody = null;
        var client = CreateClient(request =>
        {
            sentBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return FileResponse(PdfMediaType);
        });

        await DownloadAsync(client);

        Assert.Equal(Body.GetRawText(), sentBody);
    }

    [Fact]
    public async Task DownloadRejectsAnotherContentType()
    {
        var client = CreateClient(_ => FileResponse("text/html"));

        var failure = await Record.ExceptionAsync(() => DownloadAsync(client));

        Assert.IsType<InvalidOperationException>(failure);
    }

    [Fact]
    public async Task DownloadReportsApiErrorCodes()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"class":["error"],"properties":{"code":"errors.noCharactersSelected"}}""", Encoding.UTF8,
                "application/vnd.siren+json")
        });

        var failure = await Assert.ThrowsAsync<ManagementApiException>(() => DownloadAsync(client));

        Assert.Equal(("errors.noCharactersSelected", HttpStatusCode.BadRequest), (failure.ErrorCode, failure.StatusCode));
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    private static Task<byte[]> DownloadAsync(ManagementApiClient client) =>
        client.DownloadActionAsync("http://localhost/api", PrintAction, Body, PdfMediaType, "en", TestContext.Current.CancellationToken);

    private static HttpResponseMessage FileResponse(string mediaType)
    {
        var content = new ByteArrayContent(PdfBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
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
