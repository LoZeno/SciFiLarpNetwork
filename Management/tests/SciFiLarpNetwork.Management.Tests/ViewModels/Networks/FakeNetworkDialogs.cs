using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.Networks;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Networks;

/// <summary>Plays the user's part in each network dialog through the supplied callbacks.</summary>
internal sealed class FakeNetworkDialogs : INetworkDialogService
{
    public Func<NetworkEditorViewModel, Task> OnNetworkEditor { get; init; } = _ => Task.CompletedTask;

    public bool ConfirmResult { get; init; } = true;

    /// <summary>The file the user picks; <see langword="null"/> means the user cancels the picker.</summary>
    public byte[]? PickedImage { get; set; }

    public NetworkEditorViewModel? LastEditor { get; private set; }

    public ConfirmationRequest? LastConfirmation { get; private set; }

    public Task ShowNetworkEditorAsync(NetworkEditorViewModel editor)
    {
        LastEditor = editor;
        return OnNetworkEditor(editor);
    }

    public Task<bool> ConfirmAsync(ConfirmationRequest request)
    {
        LastConfirmation = request;
        return Task.FromResult(ConfirmResult);
    }

    public Task<byte[]?> PickImageAsync() => Task.FromResult(PickedImage);
}
