using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels.Players;

namespace SciFiLarpNetwork.Management.Views.Players;

public partial class PlayerEditorWindow : Window
{
    public PlayerEditorWindow()
    {
        InitializeComponent();
    }

    public PlayerEditorWindow(PlayerEditorViewModel editor)
        : this()
    {
        DataContext = editor;
        editor.CloseRequested += (_, _) => Close();
    }
}
