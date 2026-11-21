using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.Views.Hud;

namespace SciFiLarpNetwork.Management.Views;

public partial class MainWindow : Window
{
    /// <summary>
    /// The entries the navigation list shows. They follow the view model, except that a cleared list keeps its
    /// old entries until the fold-up animation ends, so the entries close up instead of vanishing at once.
    /// </summary>
    private readonly ObservableCollection<NavigationItem> _shownNavigation = [];
    private MainWindowViewModel? _viewModel;

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
            _ = HudAnimations.SlideInFromLeftAsync(Workspace);
        }
    }
}
