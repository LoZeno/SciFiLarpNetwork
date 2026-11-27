namespace SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;

/// <param name="ItemIds">The printed items, in print order.</param>
/// <param name="CopyIds">The copies made, item by item in print order.</param>
public sealed record ItemPrintDocument(Guid Id, Guid[] ItemIds, int CopiesPerItem, Guid[] CopyIds)
{
    /// <summary>The print stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
