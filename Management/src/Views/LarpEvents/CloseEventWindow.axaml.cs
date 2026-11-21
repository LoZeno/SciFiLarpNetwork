using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels.LarpEvents;

namespace SciFiLarpNetwork.Management.Views.LarpEvents;

public partial class CloseEventWindow : Window
{
    public CloseEventWindow()
    {
        InitializeComponent();
    }

    public CloseEventWindow(CloseEventViewModel editor)
        : this()
    {
        DataContext = editor;
        editor.CloseRequested += (_, _) => Close();
    }
}
