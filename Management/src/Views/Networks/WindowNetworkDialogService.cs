using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.Networks;

namespace SciFiLarpNetwork.Management.Views.Networks;

/// <summary>Shows the network dialogs as modal windows over the window returned by <paramref name="owner"/>.</summary>
public sealed class WindowNetworkDialogService(Func<Window> owner) : INetworkDialogService
{
    private static readonly FilePickerFileType ImageFiles = new("PNG, JPEG, WebP")
    {
        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp"],
        MimeTypes = ["image/png", "image/jpeg", "image/webp"]
    };

    public Task ShowNetworkEditorAsync(NetworkEditorViewModel editor) =>
        new NetworkEditorWindow(editor).ShowDialog(owner());

    public Task<bool> ConfirmAsync(ConfirmationRequest request) =>
        new ConfirmationWindow(request).ShowDialog<bool>(owner());

    /// <summary>
    /// Reads at most one byte more than the upload limit, so a huge file is not loaded into memory; the editor
    /// then rejects it as too large.
    /// </summary>
    public async Task<byte[]?> PickImageAsync()
    {
        var files = await TopLevel.GetTopLevel(owner())!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [ImageFiles]
        });
        if (files is not [var file])
        {
            return null;
        }

        await using var stream = await file.OpenReadAsync();
        var buffer = new byte[NetworkEditorViewModel.MaximumImageByteCount + 1];
        var length = 0;
        int read;
        while (length < buffer.Length && (read = await stream.ReadAsync(buffer.AsMemory(length))) > 0)
        {
            length += read;
        }

        return buffer[..length];
    }
}
