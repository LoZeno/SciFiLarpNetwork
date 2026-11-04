namespace SciFiLarpNetwork.Backend.Api.ErrorResponses;

public sealed record ApiErrorProperties(
    string Code,
    string Title,
    string Detail,
    string TraceId,
    int Status,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? Errors);
