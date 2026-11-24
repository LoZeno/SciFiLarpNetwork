using System.Net;
using System.Text;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;

namespace SciFiLarpNetwork.Management.Tests.Api;

public sealed class ManagementApiClientUploadTests : IDisposable
{
    private static readonly SirenAction Upload = new()
    {
        Name = "upload-news-media",
        Href = "/api/v1/news-media",
        Method = "POST",
        Type = "multipart/form-data"
    };

    private static readonly SirenAction Remove = new()
    {
        Name = "remove-news-media",
        Href = "/api/v1/news-media/1",
        Method = "DELETE"
    };

    private HttpClient? _httpClient;

    [Fact]
    public async Task UploadSendsTheFieldsAndTheFileAsAForm()
    {
        string? sent = null;
        var client = CreateClient(request =>
        {
            sent = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return SirenResponse();
        });

        await client.UploadFileActionAsync("http://localhost/api", Upload, new Dictionary<string, string> { ["name"] = "Harbour" },
            new UploadFile("file", "harbour.png", Encoding.ASCII.GetBytes("PNGDATA")), "en", TestContext.Current.CancellationToken);

        Assert.Equal(
            new { Name = true, FileName = true, Bytes = true },
            new
            {
                Name = sent!.Contains("name=name", StringComparison.Ordinal) && sent.Contains("Harbour", StringComparison.Ordinal),
                FileName = sent.Contains("filename=harbour.png", StringComparison.Ordinal),
                Bytes = sent.Contains("PNGDATA", StringComparison.Ordinal)
            });
    }

    [Fact]
    public async Task UploadSendsAMultipartForm()
    {
        string? mediaType = null;
        var client = CreateClient(request =>
        {
            mediaType = request.Content!.Headers.ContentType?.MediaType;
            return SirenResponse();
        });

        await client.UploadFileActionAsync("http://localhost/api", Upload, new Dictionary<string, string>(),
            new UploadFile("file", "a.png", [1]), "en", TestContext.Current.CancellationToken);

        Assert.Equal("multipart/form-data", mediaType);
    }

    [Fact]
    public async Task UploadRefusesAnActionThatIsNotAForm()
    {
        var client = CreateClient(_ => SirenResponse());

        var failure = await Record.ExceptionAsync(() => client.UploadFileActionAsync("http://localhost/api", Upload with { Type = "application/json" },
            new Dictionary<string, string>(), new UploadFile("file", "a.png", [1]), "en", TestContext.Current.CancellationToken));

        Assert.IsType<InvalidOperationException>(failure);
    }

    [Fact]
    public async Task UploadErrorIsReadAsASirenError()
    {
        var client = CreateClient(_ => ErrorResponse(HttpStatusCode.BadRequest, "errors.invalidMediaFile"));

        var failure = await Record.ExceptionAsync(() => client.UploadFileActionAsync("http://localhost/api", Upload,
            new Dictionary<string, string>(), new UploadFile("file", "a.svg", [1]), "en", TestContext.Current.CancellationToken));

        Assert.Equal("errors.invalidMediaFile", Assert.IsType<ManagementApiException>(failure).ErrorCode);
    }

    [Fact]
    public async Task ExecuteActionAcceptsAnAnswerWithNoContent()
    {
        HttpMethod? method = null;
        var client = CreateClient(request =>
        {
            method = request.Method;
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        await client.ExecuteActionAsync("http://localhost/api", Remove, "en", TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Delete, method);
    }

    [Fact]
    public async Task ExecuteActionErrorIsReadAsASirenError()
    {
        var client = CreateClient(_ => ErrorResponse(HttpStatusCode.Conflict, "errors.mediaInUse"));

        var failure = await Record.ExceptionAsync(() =>
            client.ExecuteActionAsync("http://localhost/api", Remove, "en", TestContext.Current.CancellationToken));

        Assert.Equal("errors.mediaInUse", Assert.IsType<ManagementApiException>(failure).ErrorCode);
    }

    public void Dispose() => _httpClient?.Dispose();

    private static HttpResponseMessage SirenResponse() =>
        new(HttpStatusCode.Created)
        {
            Content = new StringContent("{\"class\":[\"news-media\"],\"properties\":{}}", Encoding.UTF8, SirenMediaTypes.Json)
        };

    private static HttpResponseMessage ErrorResponse(HttpStatusCode status, string code) =>
        new(status)
        {
            Content = new StringContent(
                $"{{\"class\":[\"error\"],\"properties\":{{\"code\":\"{code}\",\"status\":{(int)status}}}}}",
                Encoding.UTF8,
                SirenMediaTypes.Json)
        };

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
