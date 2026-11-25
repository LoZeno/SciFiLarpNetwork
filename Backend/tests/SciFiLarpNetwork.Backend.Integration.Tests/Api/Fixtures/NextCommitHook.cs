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

    private Func<object, bool>? _failing;

    public void ArmAfter<T>(Action act) => _armed = (data => data is T, act);

    /// <summary>
    /// Makes the next save that appends an event of type <c>T</c> throw before anything is written, as a failed
    /// append would (Marten "Listening for Document Store Events", <c>BeforeSaveChangesAsync</c>).
    /// </summary>
    public void FailNextSaveOf<T>() => _failing = data => data is T;

    /// <summary>Drops actions that never ran, so they cannot fire in a later test.</summary>
    public void Disarm()
    {
        _armed = null;
        _failing = null;
    }

    public override Task BeforeSaveChangesAsync(IDocumentSession session, CancellationToken token)
    {
        if (_failing is { } matches && session.PendingChanges.Streams().SelectMany(stream => stream.Events).Any(e => matches(e.Data)))
        {
            _failing = null;
            throw new InvalidOperationException("The test made this save fail.");
        }

        return Task.CompletedTask;
    }

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
