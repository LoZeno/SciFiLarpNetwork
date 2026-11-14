using Avalonia.Controls;
using Avalonia.Interactivity;
using SciFiLarpNetwork.Management.ViewModels;

namespace SciFiLarpNetwork.Management.Views;

/// <summary>
/// A yes/no dialog. Its result is <see langword="true"/> only when the confirm button is used; Enter and Escape cancel.
/// </summary>
public partial class ConfirmationWindow : Window
{
    public ConfirmationWindow()
    {
        InitializeComponent();
    }

    public ConfirmationWindow(ConfirmationRequest request)
        : this()
    {
        DataContext = request;
    }

    private void OnConfirm(object? sender, RoutedEventArgs e) => Close(true);

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
