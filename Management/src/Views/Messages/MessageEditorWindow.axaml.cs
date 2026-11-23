using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels.Messages;

namespace SciFiLarpNetwork.Management.Views.Messages;

public partial class MessageEditorWindow : Window
{
    public MessageEditorWindow()
    {
        InitializeComponent();
    }

    public MessageEditorWindow(MessageEditorViewModel editor)
        : this()
    {
        DataContext = editor;
        editor.CloseRequested += (_, _) => Close();
    }
}
