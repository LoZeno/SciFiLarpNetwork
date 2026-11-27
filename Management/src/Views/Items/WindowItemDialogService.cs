using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.Items;
using SciFiLarpNetwork.Management.Views.Players;

namespace SciFiLarpNetwork.Management.Views.Items;

/// <summary>Shows the item screen's dialogs as modal windows over the window returned by <paramref name="owner"/>.</summary>
public sealed class WindowItemDialogService(Func<Window> owner) : IItemDialogService
{
    /// <summary>The picture upload limit; one byte more is read so a larger file is reported as too large.</summary>
    private const int MaximumImageByteCount = 2 * 1024 * 1024;

    private static readonly FilePickerFileType ImageFiles = new("PNG, JPEG, WebP")
    {
        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp"],
        MimeTypes = ["image/png", "image/jpeg", "image/webp"]
    };

    public Task ShowItemEditorAsync(ItemEditorViewModel editor) => new ItemEditorWindow(editor).ShowDialog(owner());

    /// <remarks>A clone's components are changed from the item dialog, so this is owned by the active window.</remarks>
    public Task ShowComponentEditorAsync(ItemComponentEditorViewModel editor) =>
        new ItemComponentEditorWindow(editor).ShowDialog(ActiveWindow());

    public Task ShowCatalogAsync(ItemCatalogViewModel catalog) => new ItemCatalogWindow(catalog).ShowDialog(owner());

    public Task ShowCatalogEntryEditorAsync(ItemCatalogEntryEditorViewModel editor) =>
        new ItemCatalogEntryWindow(editor).ShowDialog(ActiveWindow());

    /// <remarks>Confirmations may come from the catalog dialog, so they are owned by the active window.</remarks>
    public Task<bool> ConfirmAsync(ConfirmationRequest request) =>
        new ConfirmationWindow(request).ShowDialog<bool>(ActiveWindow());

    /// <summary>Reads at most one byte more than the upload limit, so a huge file is not loaded into memory.</summary>
    public async Task<byte[]?> PickImageAsync()
    {
        var files = await TopLevel.GetTopLevel(ActiveWindow())!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [ImageFiles]
        });
        if (files is not [var file])
        {
            return null;
        }

        await using var stream = await file.OpenReadAsync();
        var buffer = new byte[MaximumImageByteCount + 1];
        var length = 0;
        int read;
        while (length < buffer.Length && (read = await stream.ReadAsync(buffer.AsMemory(length))) > 0)
        {
            length += read;
        }

        return buffer[..length];
    }

    /// <remarks>The file stays in the temporary folder, because the viewer may read it after this returns.</remarks>
    public async Task<bool> OpenPdfAsync(byte[] pdf, string fileName)
    {
        var path = Path.Combine(new PdfPreviewFolders(Path.GetTempPath()).Create().FullName, fileName);
        await File.WriteAllBytesAsync(path, pdf);
        return await owner().Launcher.LaunchFileInfoAsync(new FileInfo(path));
    }

    private Window ActiveWindow()
    {
        var main = owner();
        return main.OwnedWindows.LastOrDefault(window => window.IsVisible) ?? main;
    }
}
