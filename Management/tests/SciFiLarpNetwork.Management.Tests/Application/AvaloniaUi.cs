using Avalonia.Headless;
using Avalonia.Threading;

namespace SciFiLarpNetwork.Management.Tests.Application;

/// <summary>
/// Runs a test body on the Avalonia headless UI thread, using only the official <c>Avalonia.Headless</c> package.
/// The session is created once per assembly from <see cref="AvaloniaTestApplicationAttribute"/>, and each dispatch
/// gets its own isolated application instance (the default <c>AvaloniaTestIsolationLevel.PerTest</c>), as
/// <c>[AvaloniaFact]</c> does.
/// </summary>
public static class AvaloniaUi
{
    private static HeadlessUnitTestSession Session =>
        HeadlessUnitTestSession.GetOrStartForAssembly(typeof(AvaloniaUi).Assembly);

    public static Task Run(Action body) =>
        Session.Dispatch(
            () =>
            {
                body();
                Dispatcher.UIThread.RunJobs();
            },
            TestContext.Current.CancellationToken);

    public static Task Run(Func<Task> body) =>
        Session.Dispatch(
            async () =>
            {
                await body();
                Dispatcher.UIThread.RunJobs();
                return 0;
            },
            TestContext.Current.CancellationToken);
}
