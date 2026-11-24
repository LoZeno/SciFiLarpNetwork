using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.News;

namespace SciFiLarpNetwork.Management.Views.News;

/// <summary>Shows the news dialogs as modal windows over the window returned by <paramref name="owner"/>.</summary>
public sealed class WindowNewsDialogService(Func<Window> owner) : INewsDialogService
{
    private static readonly FilePickerFileType MediaFiles = new("PNG, JPEG, WebP, GIF, WebM, MP4")
    {
        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.gif", "*.webm", "*.mp4"],
        MimeTypes = ["image/png", "image/jpeg", "image/webp", "image/gif", "video/webm", "video/mp4"]
    };

    public Task ShowStoryEditorAsync(NewsEditorViewModel editor) =>
        new NewsEditorWindow(editor).ShowDialog(owner());

    /// <summary>The library opens over the story dialog when a story is being written, otherwise over the main window.</summary>
    public Task ShowMediaLibraryAsync(NewsMediaLibraryViewModel library) =>
        new NewsMediaLibraryWindow(library).ShowDialog(ActiveOwner());

    public Task<bool> ConfirmAsync(ConfirmationRequest request) =>
        new ConfirmationWindow(request).ShowDialog<bool>(ActiveOwner());

    /// <summary>
    /// Reads at most one byte more than <paramref name="maximumByteCount"/>, so a huge file is not loaded into memory;
    /// the library then refuses it as too large.
    /// </summary>
    public async Task<PickedMediaFile?> PickMediaFileAsync(long maximumByteCount)
    {
        var files = await TopLevel.GetTopLevel(ActiveOwner())!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [MediaFiles]
        });
        if (files is not [var file])
        {
            return null;
        }

        await using var stream = await file.OpenReadAsync();
        using var content = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while (content.Length <= maximumByteCount && (read = await stream.ReadAsync(buffer)) > 0)
        {
            await content.WriteAsync(buffer.AsMemory(0, (int)Math.Min(read, maximumByteCount + 1 - content.Length)));
        }

        return new PickedMediaFile(file.Name, content.ToArray());
    }

    private Window ActiveOwner()
    {
        var main = owner();
        return main.OwnedWindows.LastOrDefault(window => window.IsVisible) is { } dialog
            ? dialog.OwnedWindows.LastOrDefault(window => window.IsVisible) ?? dialog
            : main;
    }
}
