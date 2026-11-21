using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels.LarpEvents;

namespace SciFiLarpNetwork.Management.Views.LarpEvents;

public partial class ExperienceWindow : Window
{
    public ExperienceWindow()
    {
        InitializeComponent();
    }

    public ExperienceWindow(ExperienceEditorViewModel editor)
        : this()
    {
        DataContext = editor;
        editor.CloseRequested += (_, _) => Close();
    }
}
