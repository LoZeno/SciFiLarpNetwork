using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels.Networks;

namespace SciFiLarpNetwork.Management.Views.Networks;

public partial class NetworkEditorWindow : Window
{
    public NetworkEditorWindow()
    {
        InitializeComponent();
    }

    public NetworkEditorWindow(NetworkEditorViewModel editor)
        : this()
    {
        DataContext = editor;
        editor.CloseRequested += (_, _) => Close();
    }
}
