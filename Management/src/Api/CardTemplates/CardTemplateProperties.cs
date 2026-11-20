namespace SciFiLarpNetwork.Management.Api.CardTemplates;

internal sealed record CardTemplateProperties(
    string Kind,
    string Title,
    bool IsDefault,
    string? UploadedBy,
    DateTimeOffset? UploadedAtUtc,
    long Version);
