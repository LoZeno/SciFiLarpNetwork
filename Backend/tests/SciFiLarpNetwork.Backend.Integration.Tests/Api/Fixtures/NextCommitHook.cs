using Marten;
using Marten.Services;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Api.Fixtures;

/// <summary>
/// Runs an action once, right after the next commit that appends an event of type <c>T</c>, so a test can act in the
/// moment between a committed write and the code that follows it (Marten "Listening for Document Store Events").
/// Other commits, such as the host's background work, are ignored.
/// </summary>
public sealed class NextCommitHook : DocumentSessionListenerBase
{
    private (Func<object, bool> Matches, Action Act)? _armed;

    public void ArmAfter<T>(Action act) => _armed = (data => data is T, act);

    public override Task AfterCommitAsync(IDocumentSession session, IChangeSet commit, CancellationToken token)
    {
        if (_armed is { } armed && commit.GetEvents().Any(e => armed.Matches(e.Data)))
        {
            _armed = null;
            armed.Act();
        }

        return Task.CompletedTask;
    }
}
