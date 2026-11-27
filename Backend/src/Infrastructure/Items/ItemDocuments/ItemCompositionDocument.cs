namespace SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;

/// <summary>The single item-composition stream's state: which item got a component last.</summary>
public sealed record ItemCompositionDocument(Guid Id, Guid LastChangedItemId)
{
    /// <summary>The well-known id of the one item-composition stream.</summary>
    public static readonly Guid StreamId = new("9e2b7c14-5f6a-4d83-b0e9-71a3c5d8f246");

    /// <summary>The stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
