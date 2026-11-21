using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels.LarpEvents;

namespace SciFiLarpNetwork.Management.Views.LarpEvents;

public partial class RegistrationWindow : Window
{
    public RegistrationWindow()
    {
        InitializeComponent();
    }

    public RegistrationWindow(RegistrationEditorViewModel editor)
        : this()
    {
        DataContext = editor;
        editor.CloseRequested += (_, _) => Close();
    }
}
