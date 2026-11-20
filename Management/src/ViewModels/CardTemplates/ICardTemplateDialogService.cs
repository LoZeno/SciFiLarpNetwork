namespace SciFiLarpNetwork.Management.ViewModels.CardTemplates;

/// <summary>Shows the card template screen's dialogs. Each task completes when its dialog closes.</summary>
public interface ICardTemplateDialogService
{
    Task<bool> ConfirmAsync(ConfirmationRequest request);

    /// <summary>
    /// Lets the user choose a Typst template file and returns its bytes, or <see langword="null"/> when the user
    /// cancels. At most one byte more than the upload limit is read, so a huge file is not loaded into memory.
    /// </summary>
    Task<byte[]?> PickTemplateAsync();

    /// <summary>Asks where to save the template and saves it there.</summary>
    /// <param name="fileTypeName">The localized name of the Typst file type, shown in the save dialog.</param>
    /// <returns><see langword="false"/> when the user cancels.</returns>
    Task<bool> SaveTemplateAsync(byte[] content, string suggestedFileName, string fileTypeName);

    /// <summary>Opens the PDF in the computer's default PDF viewer.</summary>
    /// <returns><see langword="false"/> when no viewer could open it.</returns>
    Task<bool> PreviewPdfAsync(byte[] pdf, string fileName);
}
