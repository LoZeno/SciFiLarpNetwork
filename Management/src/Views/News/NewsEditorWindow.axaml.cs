using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels.News;

namespace SciFiLarpNetwork.Management.Views.News;

public partial class NewsEditorWindow : Window
{
    public NewsEditorWindow()
    {
        InitializeComponent();
    }

    public NewsEditorWindow(NewsEditorViewModel editor)
        : this()
    {
        DataContext = editor;
        editor.CloseRequested += (_, _) => Close();
    }
}
