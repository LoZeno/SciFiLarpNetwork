namespace SciFiLarpNetwork.Backend.Api.CardTemplates;

public static class CardTemplateLimits
{
    /// <summary>
    /// Room for a template well over the 256 KB limit sent as base64 JSON, so a file that is too large gets the
    /// template-too-large error instead of a rejected request.
    /// </summary>
    public const long MaximumRequestBytes = 1024 * 1024;
}
