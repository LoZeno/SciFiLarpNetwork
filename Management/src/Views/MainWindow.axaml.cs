using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels;

namespace SciFiLarpNetwork.Management.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
