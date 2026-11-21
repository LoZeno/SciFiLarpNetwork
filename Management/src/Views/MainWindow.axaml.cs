using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.Views.Hud;

namespace SciFiLarpNetwork.Management.Views;

#pragma warning disable CA1001 // Avalonia windows are not IDisposable; the snapshot and swap token are released in OnClosed.
public partial class MainWindow : Window
#pragma warning restore CA1001
{
    /// <summary>
    /// The entries the navigation list shows. They follow the view model, except that a cleared list keeps its
    /// old entries until the fold-up animation ends, so the entries close up instead of vanishing at once.
    /// </summary>
    private readonly ObservableCollection<NavigationItem> _shownNavigation = [];
    private MainWindowViewModel? _viewModel;
    private CancellationTokenSource? _workspaceSwap;
    private RenderTargetBitmap? _leavingSnapshot;

    public MainWindow()
    {
        InitializeComponent();
        NavigationList.ItemsSource = _shownNavigation;
        NavigationList.ContainerPrepared += OnNavigationEntryPrepared;
        NavigationReveal.Closed += OnNavigationClosed;
    }

    public MainWindow(MainWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_viewModel is not null)
        {
            _viewModel.NavigationItems.CollectionChanged -= OnNavigationItemsChanged;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as MainWindowViewModel;
        if (_viewModel is not null)
        {
            _viewModel.NavigationItems.CollectionChanged += OnNavigationItemsChanged;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        ShowNavigation();
    }

    private void OnNavigationItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => ShowNavigation();

    private void ShowNavigation()
    {
        var items = _viewModel?.NavigationItems ?? [];
        if (items.Count == 0)
        {
            NavigationReveal.IsExpanded = false;
            return;
        }

        if (!items.Take(_shownNavigation.Count).SequenceEqual(_shownNavigation))
        {
            _shownNavigation.Clear();
        }

        foreach (var item in items.Skip(_shownNavigation.Count))
        {
            _shownNavigation.Add(item);
        }

        NavigationReveal.IsExpanded = true;
    }

    private void OnNavigationClosed(object? sender, EventArgs e)
    {
        if (_viewModel is null || _viewModel.NavigationItems.Count == 0)
        {
            _shownNavigation.Clear();
        }
    }

    private void OnNavigationEntryPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (NavigationReveal.IsOpening)
        {
            _ = HudAnimations.DropInAsync(e.Container, e.Index);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedNavigationItem) && _viewModel?.SelectedNavigationItem is not null)
        {
            _ = SwapWorkspaceAsync();
        }
    }

    /// <summary>
    /// Slides the view on screen out to the left, then the newly selected one in from the left. The selection
    /// changes before the view model opens the new screen, so a snapshot of the old view is taken here and slid
    /// out on top while the real workspace waits off the panel.
    /// </summary>
    private async Task SwapWorkspaceAsync()
    {
        _workspaceSwap?.Cancel();
        _workspaceSwap?.Dispose();
        var swap = _workspaceSwap = new CancellationTokenSource();
        var leaving = ShowLeavingWorkspace();
        var arriving = HudAnimations.SlideInFromLeftAsync(Workspace, leaving is null ? TimeSpan.Zero : HudAnimations.SlideOutDuration,
            swap.Token);
        if (leaving is not null)
        {
            await HudAnimations.SlideOutToLeftAsync(LeavingWorkspace, Workspace.Bounds.Width, swap.Token);
            // A cancelled slide currently completes inside Cancel(), before the next swap shows its snapshot; this
            // check keeps a later-completing cancelled slide from hiding that newer snapshot.
            if (!swap.IsCancellationRequested)
            {
                HideLeavingWorkspace();
            }
        }

        await arriving;
    }

    private RenderTargetBitmap? ShowLeavingWorkspace()
    {
        HideLeavingWorkspace();
        var size = Workspace.Bounds.Size;
        if (size.Width < 1 || size.Height < 1)
        {
            return null;
        }

        // Taken at 96 DPI whatever the screen scaling: Image draws a higher-DPI RenderTargetBitmap zoomed in (seen at
        // 175%), and the snapshot only shows for the 200 ms it takes to slide away.
        _leavingSnapshot = new RenderTargetBitmap(
            new PixelSize((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height)), new Vector(96, 96));
        _leavingSnapshot.Render(Workspace);
        LeavingWorkspace.Width = size.Width;
        LeavingWorkspace.Height = size.Height;
        LeavingWorkspace.Source = _leavingSnapshot;
        LeavingWorkspace.IsVisible = true;
        return _leavingSnapshot;
    }

    private void HideLeavingWorkspace()
    {
        LeavingWorkspace.IsVisible = false;
        LeavingWorkspace.Source = null;
        _leavingSnapshot?.Dispose();
        _leavingSnapshot = null;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _workspaceSwap?.Cancel();
        _workspaceSwap?.Dispose();
        HideLeavingWorkspace();
    }
}
