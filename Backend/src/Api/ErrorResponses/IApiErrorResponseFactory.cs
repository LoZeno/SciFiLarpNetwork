using Microsoft.AspNetCore.Mvc.ModelBinding;

using SciFiLarpNetwork.Backend.Api.Siren;

namespace SciFiLarpNetwork.Backend.Api.ErrorResponses;

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
