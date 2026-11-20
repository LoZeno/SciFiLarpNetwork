namespace SciFiLarpNetwork.Backend.Api.CardTemplates;

/// <param name="Kind">The kind of card the template prints: <c>character</c>.</param>
/// <param name="Title">The localized name of the card kind.</param>
/// <param name="IsDefault">Whether the default template is used.</param>
/// <param name="UploadedBy">The user name of the administrator who uploaded the template in use.</param>
/// <param name="Version">The version an upload or reset must send.</param>
public sealed record CardTemplateProperties(
    string Kind,
    string Title,
    bool IsDefault,
    string? UploadedBy,
    DateTimeOffset? UploadedAtUtc,
    long Version);
