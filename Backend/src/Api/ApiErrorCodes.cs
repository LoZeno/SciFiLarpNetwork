namespace SciFiLarpNetwork.Backend.Api;

public static class ApiErrorCodes
{
    public const string Conflict = "errors.conflict";
    public const string Forbidden = "errors.forbidden";
    public const string InternalServerError = "errors.internalServerError";
    public const string NotFound = "errors.notFound";
    public const string RequestFailed = "errors.requestFailed";
    public const string ServiceUnavailable = "errors.serviceUnavailable";
    public const string Unauthorized = "errors.unauthorized";
    public const string Validation = "errors.validation";

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
