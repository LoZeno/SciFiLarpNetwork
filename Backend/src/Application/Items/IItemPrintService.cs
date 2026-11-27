namespace SciFiLarpNetwork.Backend.Application.Items;

/// <summary>Collects the cards to print. Only <see cref="PrintAsync"/> saves anything: it creates the copies.</summary>
public interface IItemPrintService
{
    /// <summary>The most cards one request can print.</summary>
    const int MaxCards = 200;

    /// <summary>The most copies of each item one print can make.</summary>
    const int MaxCopiesPerItem = 50;

    /// <summary>The fixed QR code printed on previews; it belongs to no copy.</summary>
    const string SampleQrCode = "00000000000000000000000000000000";

    /// <summary>
    /// Creates <paramref name="copiesPerItem"/> new active copies of every item, in the requested order, each with a new
    /// QR code. An item listed twice gets copies for each time it is listed. Sending the same <paramref name="printId"/>
    /// again with the same items in the same order and the same count returns the cards of the copies already made; with
    /// different content, including another order (the order sets the card order), it reports
    /// <see cref="ItemOutcome.PrintIdInUse"/>.
    /// </summary>
    Task<ItemResult<ItemCardSet>> PrintAsync(Guid printId, IReadOnlyList<Guid> itemIds, int copiesPerItem,
        CancellationToken cancellationToken = default);

    /// <summary>The cards of existing copies, in the requested order, with their own QR codes. Saves nothing.</summary>
    Task<ItemResult<ItemCardSet>> ReprintAsync(IReadOnlyList<Guid> copyIds, CancellationToken cancellationToken = default);

    /// <summary>The cards a print would make, with <see cref="SampleQrCode"/>. Saves nothing.</summary>
    Task<ItemResult<ItemCardSet>> PreviewAsync(IReadOnlyList<Guid> itemIds, int copiesPerItem,
        CancellationToken cancellationToken = default);
}
