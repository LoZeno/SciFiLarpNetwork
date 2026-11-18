namespace SciFiLarpNetwork.Backend.Api;

public sealed class ApiException : Exception
{
    /// <param name="errors">Optional details keyed by the name of the request member they are about.</param>
    public ApiException(int statusCode, string code, string messageKey,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? errors = null)
        : base(messageKey)
    {
        if (statusCode is < StatusCodes.Status400BadRequest or > 599)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCode), "API exceptions must use an HTTP error status code.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageKey);

        StatusCode = statusCode;
        Code = code;
        MessageKey = messageKey;
        Errors = errors;
    }

    public int StatusCode { get; }

    public string Code { get; }

    public string MessageKey { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<string>>? Errors { get; }
}
