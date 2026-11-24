namespace SciFiLarpNetwork.Backend.Api.Networks;

public static class NetworkLimits
{
    /// <summary>Room for a 2 MB image sent as base64 JSON (about 2.7 MB) plus the rest of the request.</summary>
    public const long MaximumImageRequestBytes = 3 * 1024 * 1024;
}
