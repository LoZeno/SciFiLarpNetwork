using Microsoft.AspNetCore.Mvc.ModelBinding;
using SciFiLarpNetwork.Backend.Localization;

namespace SciFiLarpNetwork.Backend.Api;

public interface IApiErrorResponseFactory
{
    SirenEntity<ApiErrorProperties> Create(
        HttpContext context,
        int statusCode,
        string code,
        string messageKey,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? errors = null);

    SirenEntity<ApiErrorProperties> CreateFromModelState(
        HttpContext context,
        ModelStateDictionary modelState);
}

public sealed class ApiErrorResponseFactory(IApplicationText text) : IApiErrorResponseFactory
{
    public SirenEntity<ApiErrorProperties> Create(
        HttpContext context,
        int statusCode,
        string code,
        string messageKey,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? errors = null) =>
        new()
        {
            Class = ["error"],
            Properties = new ApiErrorProperties(
                code,
                text.GetLocalizedText("common.error"),
                text.GetLocalizedText(messageKey),
                context.TraceIdentifier,
                statusCode,
                errors),
            Links =
            [
                new SirenLink
                {
                    Rel = ["self"],
                    Href = $"{context.Request.PathBase}{context.Request.Path}"
                }
            ]
        };

    public SirenEntity<ApiErrorProperties> CreateFromModelState(
        HttpContext context,
        ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => string.IsNullOrWhiteSpace(entry.Key) ? "$" : entry.Key,
                entry => (IReadOnlyList<string>)entry.Value!.Errors
                    .Select(_ => text.GetLocalizedText("validation.invalid"))
                    .ToArray(),
                StringComparer.Ordinal);

        return Create(
            context,
            StatusCodes.Status400BadRequest,
            ApiErrorCodes.Validation,
            ApiErrorCodes.Validation,
            errors);
    }
}

public sealed record ApiErrorProperties(
    string Code,
    string Title,
    string Detail,
    string TraceId,
    int Status,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? Errors);
