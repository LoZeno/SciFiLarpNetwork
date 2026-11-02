using System.Windows.Input;
using SciFiLarpNetwork.Management.ViewModels;

namespace SciFiLarpNetwork.Management.Tests.ViewModels;

public sealed class AsyncCommandTests
{
    [Fact]
    public async Task ExecuteReportsUnhandledExceptionsToHandler()
    {
        var exception = new InvalidOperationException("Command failed.");
        var reportedException = new TaskCompletionSource<Exception>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var command = new AsyncCommand(
            () => Task.FromException(exception),
            reportedException.SetResult);

        ((ICommand)command).Execute(null);

        Assert.Same(exception, await reportedException.Task.WaitAsync(TimeSpan.FromSeconds(1)));
    }
}
