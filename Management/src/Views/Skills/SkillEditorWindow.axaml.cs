using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels.Skills;

namespace SciFiLarpNetwork.Management.Views.Skills;

public partial class SkillEditorWindow : Window
{
    public SkillEditorWindow()
    {
        InitializeComponent();
    }

    public SkillEditorWindow(SkillEditorViewModel editor)
        : this()
    {
        DataContext = editor;
        editor.CloseRequested += (_, _) => Close();
    }
}
