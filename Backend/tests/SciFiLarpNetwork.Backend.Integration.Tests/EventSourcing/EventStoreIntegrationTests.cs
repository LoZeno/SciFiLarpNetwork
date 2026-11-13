using JasperFx.Events;
using Marten;
using Marten.Events;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Tests.Integration.Api.Fixtures;

namespace SciFiLarpNetwork.Backend.Tests.Integration.EventSourcing;

[Collection("Backend database integration")]
public sealed class EventStoreIntegrationTests(ApiApplicationFactory factory) : IClassFixture<ApiApplicationFactory>
{
    [Fact]
    public async Task AsynchronousProjectionCanBeRebuiltFromCommittedEvents()
    {
        var documentStore = factory.Services.GetRequiredService<IDocumentStore>();
        var streamId = await StartProbeStreamAsync(documentStore, CreateEvent(), CreateEvent());

        await documentStore.WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(20));
        var incrementallyBuiltState = await LoadProjectionAsync(documentStore, streamId);

        using var daemon = await documentStore.BuildProjectionDaemonAsync();
        await daemon.RebuildProjectionAsync("EventStoreProbe", CancellationToken.None);

        var rebuiltState = await LoadProjectionAsync(documentStore, streamId);
        Assert.Equal(
            new
            {
                Incremental = new EventStoreProbeState(streamId, 2, 2),
                Rebuilt = new EventStoreProbeState(streamId, 2, 2)
            },
            new { Incremental = incrementallyBuiltState, Rebuilt = rebuiltState });
    }

    [Fact]
    public async Task ProjectionFailureDoesNotDiscardCommittedEventsAndCanRecoverByRebuild()
    {
        var documentStore = factory.Services.GetRequiredService<IDocumentStore>();
        EventStoreProbeProjection.FailOnMarkedEvent = true;
        try
        {
            var streamId = await StartProbeStreamAsync(documentStore, CreateEvent(causeProjectionFailure: true));
            await Task.Delay(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken);

            int committedEventCount;
            await using (var query = documentStore.QuerySession())
            {
                committedEventCount = (await query.Events.FetchStreamAsync(streamId, token: TestContext.Current.CancellationToken)).Count;
            }

            EventStoreProbeProjection.FailOnMarkedEvent = false;
            using var daemon = await documentStore.BuildProjectionDaemonAsync();
            await daemon.RebuildProjectionAsync("EventStoreProbe", CancellationToken.None);

            var recoveredState = await LoadProjectionAsync(documentStore, streamId);

            Assert.Equal(
                new ProjectionRecoverySnapshot(1, new EventStoreProbeState(streamId, 1, 1)),
                new ProjectionRecoverySnapshot(committedEventCount, recoveredState));
        }
        finally
        {
            EventStoreProbeProjection.FailOnMarkedEvent = false;
        }
    }

    private static TestProbeEvent CreateEvent(bool causeProjectionFailure = false) =>
        new("Test character", causeProjectionFailure);

    private static async Task<Guid> StartProbeStreamAsync(IDocumentStore documentStore, params object[] events)
    {
        var streamId = Guid.NewGuid();
        await using var session = documentStore.LightweightSession();
        session.Events.StartStream<EventStoreProbeState>(streamId, events);
        await session.SaveChangesAsync(TestContext.Current.CancellationToken);
        return streamId;
    }

    private static async Task<EventStoreProbeState> LoadProjectionAsync(IDocumentStore documentStore, Guid streamId)
    {
        await using var session = documentStore.QuerySession();
        return await session.LoadAsync<EventStoreProbeState>(streamId) ??
               throw new InvalidOperationException("The asynchronous event projection has not produced a state.");
    }

    private sealed record ProjectionRecoverySnapshot(int CommittedEvents, EventStoreProbeState RecoveredState);
}
