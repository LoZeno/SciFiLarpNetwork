namespace SciFiLarpNetwork.Backend.Application.CardTemplates;

/// <param name="IsDefault">Whether the default template is used: nothing was uploaded, or the template was reset.</param>
/// <param name="UploadedBy">The user name of the administrator who uploaded the current template.</param>
/// <param name="Version">The template stream version that an upload or reset must be based on; 0 before any change.</param>
public sealed record CardTemplateSummary(
    bool IsDefault,
    string? UploadedBy,
    DateTimeOffset? UploadedAtUtc,
    long Version);
