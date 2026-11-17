using Avalonia.Controls;
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

    public Task<bool> ConfirmAsync(ConfirmationRequest request) =>
        new ConfirmationWindow(request).ShowDialog<bool>(owner());
}
