using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels.Players;

namespace SciFiLarpNetwork.Management.Views.Players;

public partial class CharacterSkillsWindow : Window
{
    public CharacterSkillsWindow()
    {
        InitializeComponent();
    }

    public CharacterSkillsWindow(CharacterSkillsViewModel skills)
        : this()
    {
        DataContext = skills;
        skills.CloseRequested += (_, _) => Close();
    }
}
