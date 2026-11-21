using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.LarpEvents;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.LarpEvents;

/// <summary>Plays the user's part in each event dialog.</summary>
internal sealed class FakeLarpEventDialogs : ILarpEventDialogService
{
    public Func<LarpEventEditorViewModel, Task> OnEventEditor { get; init; } = _ => Task.CompletedTask;

    public Func<DayEditorViewModel, Task> OnDayEditor { get; init; } = _ => Task.CompletedTask;

    public Func<RegistrationEditorViewModel, Task> OnRegistration { get; init; } = _ => Task.CompletedTask;

    public Func<ExperienceEditorViewModel, Task> OnExperience { get; init; } = _ => Task.CompletedTask;

    public Func<CloseEventViewModel, Task> OnCloseEvent { get; init; } = _ => Task.CompletedTask;

    public bool ConfirmResult { get; init; } = true;

    public bool ExportResult { get; init; } = true;

    public bool PreviewResult { get; init; } = true;

    public ConfirmationRequest? LastConfirmation { get; private set; }

    public byte[]? LastExportedPdf { get; private set; }

    public byte[]? LastPreviewedPdf { get; private set; }

    public Task ShowEventEditorAsync(LarpEventEditorViewModel editor) => OnEventEditor(editor);

    public Task ShowDayEditorAsync(DayEditorViewModel editor) => OnDayEditor(editor);

    public Task ShowRegistrationAsync(RegistrationEditorViewModel editor) => OnRegistration(editor);

    public Task ShowExperienceAsync(ExperienceEditorViewModel editor) => OnExperience(editor);

    public Task ShowCloseEventAsync(CloseEventViewModel editor) => OnCloseEvent(editor);

    public Task<bool> ConfirmAsync(ConfirmationRequest request)
    {
        LastConfirmation = request;
        return Task.FromResult(ConfirmResult);
    }

    public Task<bool> ExportPdfAsync(byte[] pdf, string suggestedFileName, string fileTypeName)
    {
        LastExportedPdf = pdf;
        return Task.FromResult(ExportResult);
    }

    public Task<bool> PreviewPdfAsync(byte[] pdf, string fileName)
    {
        LastPreviewedPdf = pdf;
        return Task.FromResult(PreviewResult);
    }
}
