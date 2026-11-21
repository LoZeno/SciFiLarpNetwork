using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.LarpEvents;
using SciFiLarpNetwork.Management.Views.Players;

namespace SciFiLarpNetwork.Management.Views.LarpEvents;

/// <summary>Shows the event screen's dialogs as modal windows over the window returned by <paramref name="owner"/>.</summary>
public sealed class WindowLarpEventDialogService(Func<Window> owner) : ILarpEventDialogService
{
    public Task ShowEventEditorAsync(LarpEventEditorViewModel editor) =>
        new LarpEventEditorWindow(editor).ShowDialog(owner());

    public Task ShowDayEditorAsync(DayEditorViewModel editor) =>
        new DayEditorWindow(editor).ShowDialog(owner());

    public Task ShowRegistrationAsync(RegistrationEditorViewModel editor) =>
        new RegistrationWindow(editor).ShowDialog(owner());

    public Task ShowExperienceAsync(ExperienceEditorViewModel editor) =>
        new ExperienceWindow(editor).ShowDialog(owner());

    public Task ShowCloseEventAsync(CloseEventViewModel editor) =>
        new CloseEventWindow(editor).ShowDialog(owner());

    /// <remarks>The confirmation can open from the close dialog, so it is owned by the active window.</remarks>
    public Task<bool> ConfirmAsync(ConfirmationRequest request) =>
        new ConfirmationWindow(request).ShowDialog<bool>(ActiveWindow());

    public async Task<bool> ExportPdfAsync(byte[] pdf, string suggestedFileName, string fileTypeName)
    {
        var file = await owner().StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "pdf",
            FileTypeChoices = [new FilePickerFileType(fileTypeName) { Patterns = ["*.pdf"], MimeTypes = ["application/pdf"] }]
        });
        if (file is null)
        {
            return false;
        }

        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(pdf);
        return true;
    }

    /// <remarks>The file stays in the temporary folder, because the viewer may read it after this returns.</remarks>
    public async Task<bool> PreviewPdfAsync(byte[] pdf, string fileName)
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
