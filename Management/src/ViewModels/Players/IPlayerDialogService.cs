namespace SciFiLarpNetwork.Management.ViewModels.Players;

/// <summary>
/// Shows the player screen's dialogs. Each task completes when its dialog closes.
/// </summary>
public interface IPlayerDialogService
{
    Task ShowPlayerEditorAsync(PlayerEditorViewModel editor);

    Task ShowCharacterEditorAsync(CharacterEditorViewModel editor);

    Task ShowStatsAsync(CharacterStatsViewModel stats);

    Task ShowCharacterSkillsAsync(CharacterSkillsViewModel skills);

    Task<bool> ConfirmAsync(ConfirmationRequest request);

    /// <summary>Asks where to save the PDF and saves it there.</summary>
    /// <param name="fileTypeName">The localized name of the PDF file type, shown in the save dialog.</param>
    /// <returns><see langword="false"/> when the user cancels.</returns>
    Task<bool> ExportPdfAsync(byte[] pdf, string suggestedFileName, string fileTypeName);

    /// <summary>Opens the PDF in the computer's default PDF viewer.</summary>
    /// <returns><see langword="false"/> when no viewer could open it.</returns>
    Task<bool> PreviewPdfAsync(byte[] pdf, string fileName);
}
