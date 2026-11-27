namespace SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;

public sealed record ItemTypeDocument(Guid Id, string Name, string NormalizedName, bool IsArchived)
{
    /// <summary>The item type stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
