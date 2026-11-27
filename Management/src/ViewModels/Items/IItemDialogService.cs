namespace SciFiLarpNetwork.Management.ViewModels.Items;

/// <summary>Shows the item screen's dialogs. Each task completes when its dialog closes.</summary>
public interface IItemDialogService
{
    Task ShowItemEditorAsync(ItemEditorViewModel editor);

    Task ShowComponentEditorAsync(ItemComponentEditorViewModel editor);

    Task ShowCatalogAsync(ItemCatalogViewModel catalog);

    /// <remarks>Opened from the catalog dialog, so it is owned by that dialog.</remarks>
    Task ShowCatalogEntryEditorAsync(ItemCatalogEntryEditorViewModel editor);

    Task<bool> ConfirmAsync(ConfirmationRequest request);

    /// <summary>
    /// Lets the user choose a PNG, JPEG, or WebP file and returns its bytes, or <see langword="null"/> when the user
    /// cancels.
    /// </summary>
    Task<byte[]?> PickImageAsync();

    /// <summary>Opens the PDF in the computer's default PDF viewer.</summary>
    /// <returns><see langword="false"/> when no viewer could open it.</returns>
    Task<bool> OpenPdfAsync(byte[] pdf, string fileName);
}
