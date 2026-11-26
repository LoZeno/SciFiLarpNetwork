using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels.Items;

namespace SciFiLarpNetwork.Management.Views.Items;

public partial class ItemCatalogEntryWindow : Window
{
    public ItemCatalogEntryWindow()
    {
        InitializeComponent();
    }

    public ItemCatalogEntryWindow(ItemCatalogEntryEditorViewModel editor)
        : this()
    {
        DataContext = editor;
        editor.CloseRequested += (_, _) => Close();
    }
}
