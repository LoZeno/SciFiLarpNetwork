using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels.News;

namespace SciFiLarpNetwork.Management.Views.News;

public partial class NewsMediaLibraryWindow : Window
{
    public NewsMediaLibraryWindow()
    {
        InitializeComponent();
    }

    public NewsMediaLibraryWindow(NewsMediaLibraryViewModel library)
        : this()
    {
        DataContext = library;
        library.CloseRequested += (_, _) => Close();
    }
}
