using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels.LarpEvents;

namespace SciFiLarpNetwork.Management.Views.LarpEvents;

public partial class LarpEventEditorWindow : Window
{
    public LarpEventEditorWindow()
    {
        InitializeComponent();
    }

    public LarpEventEditorWindow(LarpEventEditorViewModel editor)
        : this()
    {
        DataContext = editor;
        editor.CloseRequested += (_, _) => Close();
    }
}
