namespace SciFiLarpNetwork.Backend.Api;

public sealed class ApiException : Exception
{
    public ApiException(int statusCode, string code, string messageKey)
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
    }

    public int StatusCode { get; }

    public string Code { get; }

    public string MessageKey { get; }
}
