namespace SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;

/// <param name="PrintedBy">The recorded actor of the print: an administrator account identifier.</param>
/// <param name="ChangedBy">The recorded actor of the last deactivation or reactivation.</param>
public sealed record ItemCopyDocument(
    Guid Id,
    Guid ItemId,
    Guid PrintId,
    DateTimeOffset PrintedAtUtc,
    string? PrintedBy,
    bool IsActive,
    DateTimeOffset? ChangedAtUtc,
    string? ChangedBy)
{
    /// <summary>The copy stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
