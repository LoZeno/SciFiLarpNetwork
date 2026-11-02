using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using SciFiLarpNetwork.Backend.Api.ErrorResponses;

namespace SciFiLarpNetwork.Backend.Api;

public sealed class SirenAuthorizationMiddlewareResultHandler(
    IApiErrorResponseFactory responseFactory) : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);

        if (context.Response.HasStarted || context.Response.ContentType is not null)
        {
            return;
        }

        var (statusCode, code) = authorizeResult switch
        {
            { Challenged: true } => (StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized),
            { Forbidden: true } => (StatusCodes.Status403Forbidden, ApiErrorCodes.Forbidden),
            _ => (0, string.Empty)
        };

        if (statusCode == 0)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = SirenMediaTypes.Json;
        var response = responseFactory.Create(context, statusCode, code, code);
        await SirenJson.SerializeAsync(context.Response.Body, response, context.RequestAborted);
    }
}
