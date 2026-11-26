namespace SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;

public sealed record RarityDocument(Guid Id, string Name, string NormalizedName, bool IsArchived)
{
    /// <summary>The rarity stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
