namespace SciFiLarpNetwork.Backend.Infrastructure.Skills.SkillDocuments;

public sealed record TalentDocument(
    Guid Id,
    string Name,
    string NormalizedName,
    string? Description,
    bool IsArchived)
{
    /// <summary>The talent stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
