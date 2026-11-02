using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api;

public static class ApiErrorCodes
{
    public const string Conflict = SirenErrorCodes.Conflict;
    public const string Forbidden = SirenErrorCodes.Forbidden;
    public const string InternalServerError = SirenErrorCodes.InternalServerError;
    public const string NotFound = SirenErrorCodes.NotFound;
    public const string RequestFailed = SirenErrorCodes.RequestFailed;
    public const string ServiceUnavailable = SirenErrorCodes.ServiceUnavailable;
    public const string Unauthorized = SirenErrorCodes.Unauthorized;
    public const string Validation = SirenErrorCodes.Validation;

    public static (string Code, string MessageKey) FromStatusCode(int statusCode) =>
        statusCode switch
        {
            StatusCodes.Status400BadRequest => (Validation, Validation),
            StatusCodes.Status401Unauthorized => (Unauthorized, Unauthorized),
            StatusCodes.Status403Forbidden => (Forbidden, Forbidden),
            StatusCodes.Status404NotFound => (NotFound, NotFound),
            StatusCodes.Status409Conflict => (Conflict, Conflict),
            StatusCodes.Status503ServiceUnavailable => (ServiceUnavailable, ServiceUnavailable),
            >= StatusCodes.Status500InternalServerError and <= 599 => (InternalServerError, InternalServerError),
            _ => (RequestFailed, RequestFailed)
        };
}
