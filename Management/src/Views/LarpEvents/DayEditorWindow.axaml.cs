using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels.LarpEvents;

namespace SciFiLarpNetwork.Management.Views.LarpEvents;

public partial class DayEditorWindow : Window
{
    public DayEditorWindow()
    {
        InitializeComponent();
    }

    public DayEditorWindow(DayEditorViewModel editor)
        : this()
    {
        DataContext = editor;
        editor.CloseRequested += (_, _) => Close();
    }
}
