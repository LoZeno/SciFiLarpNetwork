using Microsoft.AspNetCore.Diagnostics;

namespace SciFiLarpNetwork.Backend.Api;

public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger,
    IApiErrorResponseFactory responseFactory) : IExceptionHandler
{
    private static readonly Action<ILogger, string, Exception?> ResponseAlreadyStartedLog =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1, nameof(ResponseAlreadyStartedLog)),
            "An API exception occurred after the response started. TraceId: {TraceId}.");

    private static readonly Action<ILogger, string, string, Exception?> ApiRequestFailedLog =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(2, nameof(ApiRequestFailedLog)),
            "API request failed with {ErrorCode}. TraceId: {TraceId}.");

    private static readonly Action<ILogger, string, Exception?> UnhandledExceptionLog =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(3, nameof(UnhandledExceptionLog)),
            "An unhandled API exception occurred. TraceId: {TraceId}.");

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            ResponseAlreadyStartedLog(logger, httpContext.TraceIdentifier, exception);
            return false;
        }

        var (statusCode, code, messageKey) = exception is ApiException apiException
            ? (apiException.StatusCode, apiException.Code, apiException.MessageKey)
            : (StatusCodes.Status500InternalServerError,
                ApiErrorCodes.InternalServerError,
                ApiErrorCodes.InternalServerError);

        if (exception is ApiException)
        {
            ApiRequestFailedLog(logger, code, httpContext.TraceIdentifier, null);
        }
        else
        {
            UnhandledExceptionLog(logger, httpContext.TraceIdentifier, exception);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = SirenMediaTypes.Json;
        var response = responseFactory.Create(httpContext, statusCode, code, messageKey);
        await SirenJson.SerializeAsync(httpContext.Response.Body, response, cancellationToken);

        return true;
    }
}
