namespace SciFiLarpNetwork.Backend.Infrastructure.Skills.SkillDocuments;

public sealed record CardFlagDocument(
    Guid Id,
    string Name,
    string NormalizedName)
{
    /// <summary>The card flag stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
