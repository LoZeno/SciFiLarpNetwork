using System.Net;

namespace SciFiLarpNetwork.Management.Api;

public sealed class ManagementApiException(
    string errorCode,
    HttpStatusCode statusCode) : Exception(errorCode)
{
    public string ErrorCode { get; } = errorCode;

    public HttpStatusCode StatusCode { get; } = statusCode;
}
