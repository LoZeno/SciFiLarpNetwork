using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels.Players;

namespace SciFiLarpNetwork.Management.Views.Players;

public partial class CharacterEditorWindow : Window
{
    public CharacterEditorWindow()
    {
        InitializeComponent();
    }

    public CharacterEditorWindow(CharacterEditorViewModel editor)
        : this()
    {
        DataContext = editor;
        editor.CloseRequested += (_, _) => Close();
    }
}
