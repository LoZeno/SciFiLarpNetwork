using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels.Messages;

namespace SciFiLarpNetwork.Management.Views.Messages;

/// <summary>Shows the message screen's dialog as a modal window over the window returned by <paramref name="owner"/>.</summary>
public sealed class WindowMessageDialogService(Func<Window> owner) : IMessageDialogService
{
    public Task ShowMessageEditorAsync(MessageEditorViewModel editor) =>
        new MessageEditorWindow(editor).ShowDialog(owner());
}
