using Microsoft.AspNetCore.Mvc.ModelBinding;
using SciFiLarpNetwork.Backend.Api.Siren;
using SciFiLarpNetwork.Backend.Localization.Text;

namespace SciFiLarpNetwork.Backend.Api.ErrorResponses;

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
