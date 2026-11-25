using Avalonia.Controls;

namespace SciFiLarpNetwork.Management.Views.News;

/// <summary>Draws a story as players will see it, from the preview blocks it lists.</summary>
public partial class NewsPreviewView : ItemsControl
{
    public NewsPreviewView()
    {
        InitializeComponent();
    }

    protected override Type StyleKeyOverride => typeof(ItemsControl);
}
