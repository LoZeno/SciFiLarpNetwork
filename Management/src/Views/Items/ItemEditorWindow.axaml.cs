using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels.Items;

namespace SciFiLarpNetwork.Management.Views.Items;

public partial class ItemEditorWindow : Window
{
    public ItemEditorWindow()
    {
        InitializeComponent();
    }

    public ItemEditorWindow(ItemEditorViewModel editor)
        : this()
    {
        DataContext = editor;
        editor.CloseRequested += (_, _) => Close();
    }
}
