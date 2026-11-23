namespace SciFiLarpNetwork.Management.ViewModels.Messages;

/// <summary>Shows the message screen's dialog. The task completes when the dialog closes.</summary>
public interface IMessageDialogService
{
    Task ShowMessageEditorAsync(MessageEditorViewModel editor);
}
