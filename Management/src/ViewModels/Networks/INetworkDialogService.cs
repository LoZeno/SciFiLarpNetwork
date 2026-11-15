namespace SciFiLarpNetwork.Management.ViewModels.Networks;

/// <summary>
/// Shows the network dialogs. The editor task completes when its dialog closes.
/// </summary>
public interface INetworkDialogService
{
    Task ShowNetworkEditorAsync(NetworkEditorViewModel editor);

    Task<bool> ConfirmAsync(ConfirmationRequest request);

    /// <summary>
    /// Lets the user choose a PNG, JPEG, or WebP file and returns its bytes, or <see langword="null"/> when the
    /// user cancels.
    /// </summary>
    Task<byte[]?> PickImageAsync();
}
