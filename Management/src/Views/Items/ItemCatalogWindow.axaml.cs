using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels.Items;

namespace SciFiLarpNetwork.Management.Views.Items;

public partial class ItemCatalogWindow : Window
{
    public ItemCatalogWindow()
    {
        InitializeComponent();
    }

    public ItemCatalogWindow(ItemCatalogViewModel catalog)
        : this()
    {
        DataContext = catalog;
        catalog.CloseRequested += (_, _) => Close();
    }
}
