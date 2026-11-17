namespace SciFiLarpNetwork.Management.ViewModels.Players;

/// <summary>
/// Shows the player screen's dialogs. Each task completes when its dialog closes.
/// </summary>
public interface IPlayerDialogService
{
    Task ShowPlayerEditorAsync(PlayerEditorViewModel editor);

    Task ShowCharacterEditorAsync(CharacterEditorViewModel editor);

    Task ShowStatsAsync(CharacterStatsViewModel stats);

    Task<bool> ConfirmAsync(ConfirmationRequest request);
}
