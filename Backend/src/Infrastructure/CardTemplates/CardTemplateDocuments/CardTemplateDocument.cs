namespace SciFiLarpNetwork.Backend.Infrastructure.CardTemplates.CardTemplateDocuments;

/// <summary>The state of one card kind's template stream.</summary>
/// <param name="ContentId">The uploaded template in use, or <see langword="null"/> for the default template.</param>
/// <param name="UploadedBy">The actor that uploaded the template in use: the event's user name metadata.</param>
public sealed record CardTemplateDocument(
    Guid Id,
    Guid? ContentId,
    string? UploadedBy,
    DateTimeOffset? UploadedAtUtc)
{
    /// <summary>The well-known id of the character-card template stream.</summary>
    public static readonly Guid CharacterStreamId = new("3d0f6a52-8c1e-4b7d-9a35-c2e4f1b08d67");

    /// <summary>The stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
