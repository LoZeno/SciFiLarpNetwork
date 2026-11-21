namespace SciFiLarpNetwork.Management.ViewModels.LarpEvents;

/// <summary>Shows the event screen's dialogs. Each task completes when its dialog closes.</summary>
public interface ILarpEventDialogService
{
    Task ShowEventEditorAsync(LarpEventEditorViewModel editor);

    Task ShowDayEditorAsync(DayEditorViewModel editor);

    Task ShowRegistrationAsync(RegistrationEditorViewModel editor);

    Task ShowExperienceAsync(ExperienceEditorViewModel editor);

    Task ShowCloseEventAsync(CloseEventViewModel editor);

    Task<bool> ConfirmAsync(ConfirmationRequest request);

    /// <summary>Asks where to save the PDF and saves it there.</summary>
    /// <returns><see langword="false"/> when the user cancels.</returns>
    Task<bool> ExportPdfAsync(byte[] pdf, string suggestedFileName, string fileTypeName);

    /// <summary>Opens the PDF in the computer's default PDF viewer.</summary>
    /// <returns><see langword="false"/> when no viewer could open it.</returns>
    Task<bool> PreviewPdfAsync(byte[] pdf, string fileName);
}
