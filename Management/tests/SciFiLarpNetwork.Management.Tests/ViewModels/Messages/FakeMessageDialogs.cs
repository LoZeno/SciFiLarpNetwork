using SciFiLarpNetwork.Management.ViewModels.Messages;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Messages;

/// <summary>Plays the user's part in the message dialog.</summary>
internal sealed class FakeMessageDialogs : IMessageDialogService
{
    public Func<MessageEditorViewModel, Task> OnMessageEditor { get; init; } = _ => Task.CompletedTask;

    public MessageEditorViewModel? LastEditor { get; private set; }

    public Task ShowMessageEditorAsync(MessageEditorViewModel editor)
    {
        LastEditor = editor;
        return OnMessageEditor(editor);
    }
}
