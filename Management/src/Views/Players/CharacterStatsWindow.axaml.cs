using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels.Players;

namespace SciFiLarpNetwork.Management.Views.Players;

public partial class CharacterStatsWindow : Window
{
    public CharacterStatsWindow()
    {
        InitializeComponent();
    }

    public CharacterStatsWindow(CharacterStatsViewModel stats)
        : this()
    {
        DataContext = stats;
        stats.CloseRequested += (_, _) => Close();
    }
}
