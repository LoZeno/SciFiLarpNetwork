using System.Net;

namespace SciFiLarpNetwork.Management.Api;

/// <param name="errors">Error details keyed by the request member they are about; empty when the API sent none.</param>
public sealed class ManagementApiException(
    string errorCode,
    HttpStatusCode statusCode,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? errors = null) : Exception(errorCode)
{
    public string ErrorCode { get; } = errorCode;

    public HttpStatusCode StatusCode { get; } = statusCode;

    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; } =
        errors ?? new Dictionary<string, IReadOnlyList<string>>();
}
