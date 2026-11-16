using System.Net;
using System.Net.Http.Headers;
using System.Text;
using SciFiLarpNetwork.Management.Api;

namespace SciFiLarpNetwork.Management.Tests.Api;

public sealed class ManagementApiClientImageTests : IDisposable
{
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01];
    private HttpClient? _httpClient;

    [Fact]
    public async Task GetImageAsyncReturnsTheImageBytes()
    {
        var client = CreateClient(_ => ImageResponse("image/png"));

        var content = await client.GetImageAsync("http://localhost/api", "/api/v1/network-images/1", "en", TestContext.Current.CancellationToken);

        Assert.Equal(PngBytes, content);
    }

    [Fact]
    public async Task GetImageAsyncAsksOnlyForSupportedImageTypes()
    {
        HttpRequestMessage? sent = null;
        var client = CreateClient(request =>
        {
            sent = request;
            return ImageResponse("image/webp");
        });

        await client.GetImageAsync("http://localhost/api", "/api/v1/network-images/1", "it", TestContext.Current.CancellationToken);

        Assert.Equal("image/png,image/jpeg,image/webp", string.Join(',', sent!.Headers.Accept.Select(header => header.MediaType)));
    }

    [Theory]
    [InlineData("image/svg+xml")]
    [InlineData("text/html")]
    public async Task GetImageAsyncRejectsOtherContentTypes(string mediaType)
    {
        var client = CreateClient(_ => ImageResponse(mediaType));

        var failure = await Record.ExceptionAsync(() =>
            client.GetImageAsync("http://localhost/api", "/api/v1/network-images/1", "en", TestContext.Current.CancellationToken));

        Assert.IsType<InvalidOperationException>(failure);
    }

    [Fact]
    public async Task GetImageAsyncRefusesTargetsOutsideTheApi()
    {
        var client = CreateClient(_ => throw new InvalidOperationException("No request expected."));

        var failure = await Record.ExceptionAsync(() =>
            client.GetImageAsync("http://localhost/api", "https://outside.invalid/image.png", "en", TestContext.Current.CancellationToken));

        Assert.IsType<InvalidOperationException>(failure);
    }

    [Fact]
    public async Task GetImageAsyncReportsApiErrorCodes()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("""{"class":["error"],"properties":{"code":"errors.notFound"}}""", Encoding.UTF8, "application/vnd.siren+json")
        });

        var failure = await Assert.ThrowsAsync<ManagementApiException>(() =>
            client.GetImageAsync("http://localhost/api", "/api/v1/network-images/1", "en", TestContext.Current.CancellationToken));

        Assert.Equal(("errors.notFound", HttpStatusCode.NotFound), (failure.ErrorCode, failure.StatusCode));
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    private static HttpResponseMessage ImageResponse(string mediaType)
    {
        var content = new ByteArrayContent(PngBytes);
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
