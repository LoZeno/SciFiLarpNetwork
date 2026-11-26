namespace SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;

/// <summary>The single rarity-order stream's state: every rarity, first one first.</summary>
public sealed record RarityOrderDocument(Guid Id, Guid[] RarityIds)
{
    /// <summary>The well-known id of the one rarity-order stream.</summary>
    public static readonly Guid StreamId = new("4c8a2f61-7d3e-4b90-a5c1-2e6f8d0b9a37");

    /// <summary>The stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
