using System.Windows.Input;

namespace SciFiLarpNetwork.Management.ViewModels;

public sealed class AsyncCommand(
    Func<Task> execute,
    Action<Exception> onException,
    Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public async void Execute(object? parameter)
    {
        try
        {
            await ExecuteAsync();
        }
        catch (Exception exception)
        {
            onException(exception);
        }
    }

    public Task ExecuteAsync() => CanExecute(null) ? execute() : Task.CompletedTask;

    public void NotifyCanExecuteChanged() =>
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
