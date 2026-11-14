using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels.Skills;

namespace SciFiLarpNetwork.Management.Views.Skills;

public partial class TalentEditorWindow : Window
{
    public TalentEditorWindow()
    {
        InitializeComponent();
    }

    public TalentEditorWindow(TalentEditorViewModel editor)
        : this()
    {
        DataContext = editor;
        editor.CloseRequested += (_, _) => Close();
    }
}
