using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.CardTemplates;
using SciFiLarpNetwork.Management.Views.Players;

namespace SciFiLarpNetwork.Management.Views.CardTemplates;

/// <summary>Shows the card template dialogs as modal windows over the window returned by <paramref name="owner"/>.</summary>
public sealed class WindowCardTemplateDialogService(Func<Window> owner) : ICardTemplateDialogService
{
    private static readonly FilePickerFileType TypstFiles = new("Typst (.typ)") { Patterns = ["*.typ"] };

    public Task<bool> ConfirmAsync(ConfirmationRequest request) =>
        new ConfirmationWindow(request).ShowDialog<bool>(owner());

    public async Task<byte[]?> PickTemplateAsync()
    {
        var files = await owner().StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [TypstFiles]
        });
        if (files is not [var file])
        {
            return null;
        }

        await using var stream = await file.OpenReadAsync();
        var buffer = new byte[CardTemplateScreenViewModel.MaximumTemplateByteCount + 1];
        var length = 0;
        int read;
        while (length < buffer.Length && (read = await stream.ReadAsync(buffer.AsMemory(length))) > 0)
        {
            length += read;
        }

        return buffer[..length];
    }

    public async Task<bool> SaveTemplateAsync(byte[] content, string suggestedFileName, string fileTypeName)
    {
        var file = await owner().StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "typ",
            FileTypeChoices = [new FilePickerFileType(fileTypeName) { Patterns = ["*.typ"] }]
        });
        if (file is null)
        {
            return false;
        }

        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(content);
        return true;
    }

    /// <remarks>The file stays in the temporary folder, because the viewer may read it after this returns.</remarks>
    public async Task<bool> PreviewPdfAsync(byte[] pdf, string fileName)
    {
        var path = Path.Combine(new PdfPreviewFolders(Path.GetTempPath()).Create().FullName, fileName);
        await File.WriteAllBytesAsync(path, pdf);
        return await owner().Launcher.LaunchFileInfoAsync(new FileInfo(path));
    }
}
