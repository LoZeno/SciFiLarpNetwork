using Avalonia.Headless;

namespace SciFiLarpNetwork.Management.Tests.Application;

public sealed class AvaloniaHeadlessFixture : IDisposable
{
    public AvaloniaHeadlessFixture() =>
        Session = HeadlessUnitTestSession.StartNew(typeof(SciFiLarpNetwork.Management.App));

    public HeadlessUnitTestSession Session { get; }

    public void Dispose() => Session.Dispose();
}
