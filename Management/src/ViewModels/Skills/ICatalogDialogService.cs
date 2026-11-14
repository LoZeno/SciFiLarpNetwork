namespace SciFiLarpNetwork.Management.ViewModels.Skills;

/// <summary>
/// Shows the talent and skill dialogs. Each editor task completes when its dialog closes.
/// </summary>
public interface ICatalogDialogService
{
    Task ShowTalentEditorAsync(TalentEditorViewModel editor);

    Task ShowSkillEditorAsync(SkillEditorViewModel editor);

    Task<bool> ConfirmAsync(ConfirmationRequest request);
}
