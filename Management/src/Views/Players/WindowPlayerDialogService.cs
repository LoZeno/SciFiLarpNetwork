using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.Players;

namespace SciFiLarpNetwork.Management.Views.Players;

/// <summary>Shows the player screen's dialogs as modal windows over the window returned by <paramref name="owner"/>.</summary>
public sealed class WindowPlayerDialogService(Func<Window> owner) : IPlayerDialogService
{
    public Task ShowPlayerEditorAsync(PlayerEditorViewModel editor) =>
        new PlayerEditorWindow(editor).ShowDialog(owner());

    public Task ShowCharacterEditorAsync(CharacterEditorViewModel editor) =>
        new CharacterEditorWindow(editor).ShowDialog(owner());

    public Task ShowStatsAsync(CharacterStatsViewModel stats) =>
        new CharacterStatsWindow(stats).ShowDialog(owner());

    /// <remarks>The window opens from the character dialog, so it is owned by that dialog, the active window.</remarks>
    public Task ShowCharacterSkillsAsync(CharacterSkillsViewModel skills) =>
        new CharacterSkillsWindow(skills).ShowDialog(ActiveWindow());

    public Task<bool> ConfirmAsync(ConfirmationRequest request) =>
        new ConfirmationWindow(request).ShowDialog<bool>(owner());

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
        var path = Path.Combine(Directory.CreateTempSubdirectory("scifilarp-cards-").FullName, fileName);
        await File.WriteAllBytesAsync(path, pdf);
        return await owner().Launcher.LaunchFileInfoAsync(new FileInfo(path));
    }

    private Window ActiveWindow()
    {
        var main = owner();
        return main.OwnedWindows.LastOrDefault(window => window.IsVisible) ?? main;
    }
}
