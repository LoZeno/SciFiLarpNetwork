using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.Players;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Players;

/// <summary>Plays the user's part in each player-screen dialog through the supplied callbacks.</summary>
internal sealed class FakePlayerDialogs : IPlayerDialogService
{
    public Func<PlayerEditorViewModel, Task> OnPlayerEditor { get; init; } = _ => Task.CompletedTask;

    public Func<CharacterEditorViewModel, Task> OnCharacterEditor { get; init; } = _ => Task.CompletedTask;

    public Func<CharacterStatsViewModel, Task> OnStats { get; init; } = _ => Task.CompletedTask;

    public bool ConfirmResult { get; init; } = true;

    public CharacterEditorViewModel? LastCharacterEditor { get; private set; }

    public ConfirmationRequest? LastConfirmation { get; private set; }

    public Task ShowPlayerEditorAsync(PlayerEditorViewModel editor) => OnPlayerEditor(editor);

    public Task ShowCharacterEditorAsync(CharacterEditorViewModel editor)
    {
        LastCharacterEditor = editor;
        return OnCharacterEditor(editor);
    }

    public Task ShowStatsAsync(CharacterStatsViewModel stats) => OnStats(stats);

    public Task<bool> ConfirmAsync(ConfirmationRequest request)
    {
        LastConfirmation = request;
        return Task.FromResult(ConfirmResult);
    }
}
