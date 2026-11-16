namespace SciFiLarpNetwork.Backend.Infrastructure.Skills.SkillDocuments;

/// <summary>The single prerequisite-graph stream's state: which skill's prerequisites changed last.</summary>
public sealed record PrerequisiteGraphDocument(Guid Id, Guid LastChangedSkillId)
{
    /// <summary>The well-known id of the one prerequisite-graph stream.</summary>
    public static readonly Guid StreamId = new("b3d7e5a1-92c4-4f0e-8a6b-1d2c3e4f5a60");

    /// <summary>The stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
