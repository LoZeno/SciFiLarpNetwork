using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels.Items;

namespace SciFiLarpNetwork.Management.Views.Items;

public partial class ItemComponentEditorWindow : Window
{
    public ItemComponentEditorWindow()
    {
        InitializeComponent();
    }

    public ItemComponentEditorWindow(ItemComponentEditorViewModel editor)
        : this()
    {
        DataContext = editor;
        editor.CloseRequested += (_, _) => Close();
    }
}
