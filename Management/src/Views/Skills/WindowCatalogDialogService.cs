using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.Skills;

namespace SciFiLarpNetwork.Management.Views.Skills;

/// <summary>Shows the catalog dialogs as modal windows over the window returned by <paramref name="owner"/>.</summary>
public sealed class WindowCatalogDialogService(Func<Window> owner) : ICatalogDialogService
{
    public Task ShowTalentEditorAsync(TalentEditorViewModel editor) =>
        new TalentEditorWindow(editor).ShowDialog(owner());

    public Task ShowSkillEditorAsync(SkillEditorViewModel editor) =>
        new SkillEditorWindow(editor).ShowDialog(owner());

    public Task<bool> ConfirmAsync(ConfirmationRequest request) =>
        new ConfirmationWindow(request).ShowDialog<bool>(owner());
}
