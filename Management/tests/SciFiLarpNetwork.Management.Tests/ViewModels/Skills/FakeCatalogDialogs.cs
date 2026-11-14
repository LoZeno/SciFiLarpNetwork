using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.Skills;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Skills;

/// <summary>Plays the user's part in each dialog through the supplied callbacks.</summary>
internal sealed class FakeCatalogDialogs : ICatalogDialogService
{
    public Func<TalentEditorViewModel, Task> OnTalentEditor { get; init; } = _ => Task.CompletedTask;

    public Func<SkillEditorViewModel, Task> OnSkillEditor { get; init; } = _ => Task.CompletedTask;

    public bool ConfirmResult { get; init; } = true;

    public TalentEditorViewModel? LastTalentEditor { get; private set; }

    public SkillEditorViewModel? LastSkillEditor { get; private set; }

    public ConfirmationRequest? LastConfirmation { get; private set; }

    public Task ShowTalentEditorAsync(TalentEditorViewModel editor)
    {
        LastTalentEditor = editor;
        return OnTalentEditor(editor);
    }

    public Task ShowSkillEditorAsync(SkillEditorViewModel editor)
    {
        LastSkillEditor = editor;
        return OnSkillEditor(editor);
    }

    public Task<bool> ConfirmAsync(ConfirmationRequest request)
    {
        LastConfirmation = request;
        return Task.FromResult(ConfirmResult);
    }
}
