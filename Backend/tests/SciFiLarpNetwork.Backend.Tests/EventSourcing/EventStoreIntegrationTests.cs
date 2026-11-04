using System.Text.Json;
using JasperFx.Events;
using Marten;
using Marten.Events;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.EventSourcing;
using SciFiLarpNetwork.Backend.Domain.Events;
using SciFiLarpNetwork.Backend.Tests.Api.Fixtures;

namespace SciFiLarpNetwork.Backend.Tests.EventSourcing;

public sealed class EventStoreIntegrationTests(ApiApplicationFactory factory) : IClassFixture<ApiApplicationFactory>
{
    [Fact]
    public async Task AppendAndReadPreservesOrderedEventsAndMetadata()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();
        var firstEvent = CreateEvent(actorId: "admin-1", correlationId: "correlation-1");
        var secondEvent = CreateEvent(actorId: "admin-1", correlationId: "correlation-1", causationId: "cause-1");

        var append = await store.AppendAsync(streamId, 0, [firstEvent, secondEvent]);
        var events = await store.ReadStreamAsync(streamId);

        Assert.Equal(2, append.StreamVersion);
        Assert.False(append.WasDuplicate);
        Assert.Equal([firstEvent.EventId, secondEvent.EventId], append.EventIds);
        Assert.Equal(new long[] { 1, 2 }, events.Select(domainEvent => domainEvent.Sequence));
        Assert.Equal(streamId, events[0].StreamId);
        Assert.Equal(firstEvent.EventId, events[0].EventId);
        Assert.Equal("character.registered", events[0].EventType);
        Assert.Equal(1, events[0].SchemaVersion);
        Assert.Equal("admin-1", events[0].ActorId);
        Assert.Equal("correlation-1", events[0].CorrelationId);
        Assert.Equal("cause-1", events[1].CausationId);
        Assert.Equal(
            firstEvent.Payload.GetProperty("displayName").GetString(),
            events[0].Payload.GetProperty("displayName").GetString());
        Assert.Equal(TimeSpan.Zero, events[0].OccurredAtUtc.Offset);
        Assert.Equal(TimeSpan.Zero, events[0].RecordedAtUtc.Offset);

        var documentStore = factory.Services.GetRequiredService<IDocumentStore>();
        await using var session = documentStore.QuerySession();
        var providerEvents = await session.Events.FetchStreamAsync(streamId);
        Assert.Equal("admin-1", providerEvents[0].UserName);
        Assert.Equal("correlation-1", providerEvents[0].CorrelationId);
        Assert.Equal("cause-1", providerEvents[1].CausationId);
    }

    [Fact]
    public async Task StaleExpectedVersionRejectsAppendWithoutChangingTheStream()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();
        var originalEvent = CreateEvent();
        await store.AppendAsync(streamId, 0, [originalEvent]);

        await Assert.ThrowsAsync<StreamConcurrencyException>(
            () => store.AppendAsync(streamId, 0, [CreateEvent()]));

        var events = await store.ReadStreamAsync(streamId);
        Assert.Single(events);
        Assert.Equal(originalEvent.EventId, events[0].EventId);
    }

    [Fact]
    public async Task AppendAtIncorrectNonzeroVersionIsRejected()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();
        await store.AppendAsync(streamId, 0, [CreateEvent()]);

        await Assert.ThrowsAsync<StreamConcurrencyException>(
            () => store.AppendAsync(streamId, 2, [CreateEvent()]));
        Assert.Single(await store.ReadStreamAsync(streamId));
    }

    [Fact]
    public async Task SimultaneousWritesFromOneVersionCommitOnlyOneBatch()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();

        var writes = await Task.WhenAll(
            CaptureWriteAsync(store, streamId, CreateEvent()),
            CaptureWriteAsync(store, streamId, CreateEvent()));

        Assert.Single(writes, result => result is null);
        Assert.Single(writes, result => result is StreamConcurrencyException);
        Assert.Single(await store.ReadStreamAsync(streamId));
    }

    [Fact]
    public async Task CommandIdMakesSequentialRetriesIdempotent()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();
        var commandId = Guid.NewGuid();
        var domainEvent = CreateEvent();

        var firstResult = await store.AppendAsync(streamId, 0, [domainEvent], commandId);
        var retryResult = await store.AppendAsync(streamId, 0, [domainEvent], commandId);
        var events = await store.ReadStreamAsync(streamId);

        Assert.False(firstResult.WasDuplicate);
        Assert.True(retryResult.WasDuplicate);
        Assert.Equal(firstResult.StreamVersion, retryResult.StreamVersion);
        Assert.Equal(firstResult.EventIds.ToArray(), retryResult.EventIds.ToArray());
        Assert.Single(events);
    }

    [Fact]
    public async Task CommandIdCannotBeReusedForDifferentIntent()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();
        var commandId = Guid.NewGuid();
        await store.AppendAsync(streamId, 0, [CreateEvent()], commandId);

        await Assert.ThrowsAsync<IdempotencyKeyReuseException>(
            () => store.AppendAsync(streamId, 1, [CreateEvent()], commandId));
        Assert.Single(await store.ReadStreamAsync(streamId));
    }

    [Fact]
    public async Task EventIdsAreGloballyUniqueAndFailedBatchesAreAtomic()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var originalStreamId = Guid.NewGuid();
        var targetStreamId = Guid.NewGuid();
        var originalEvent = CreateEvent();
        await store.AppendAsync(originalStreamId, 0, [originalEvent]);

        var newEvent = CreateEvent();
        var duplicateEvent = new DomainEvent(
            originalEvent.EventId,
            originalEvent.EventType,
            originalEvent.SchemaVersion,
            originalEvent.OccurredAtUtc,
            originalEvent.Payload);
        await Assert.ThrowsAsync<JasperFx.DocumentAlreadyExistsException>(
            () => store.AppendAsync(targetStreamId, 0, [newEvent, duplicateEvent]));

        Assert.Empty(await store.ReadStreamAsync(targetStreamId));
        Assert.Single(await store.ReadStreamAsync(originalStreamId));
    }

    [Fact]
    public async Task EmptyStreamReadsAsEmptyAndAWriteWithNoEventsIsRejected()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();

        Assert.Empty(await store.ReadStreamAsync(streamId));
        await Assert.ThrowsAsync<ArgumentException>(() => store.AppendAsync(streamId, 0, []));
        Assert.Empty(await store.ReadStreamAsync(streamId));
    }

    [Fact]
    public async Task AsynchronousProjectionCanBeRebuiltFromCommittedEvents()
    {
        var eventStore = factory.Services.GetRequiredService<IEventStreamStore>();
        var documentStore = factory.Services.GetRequiredService<IDocumentStore>();
        var streamId = Guid.NewGuid();
        await eventStore.AppendAsync(streamId, 0, [CreateEvent(), CreateEvent()]);

        await documentStore.WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(20));
        var incrementallyBuiltState = await LoadProjectionAsync(documentStore, streamId);
        Assert.Equal(2, incrementallyBuiltState.EventCount);

        using var daemon = await documentStore.BuildProjectionDaemonAsync();
        await daemon.RebuildProjectionAsync("EventStoreProbe", CancellationToken.None);

        var rebuiltState = await LoadProjectionAsync(documentStore, streamId);
        Assert.Equal(incrementallyBuiltState, rebuiltState);
    }

    [Fact]
    public async Task ProjectionFailureDoesNotDiscardCommittedEventsAndCanRecoverByRebuild()
    {
        var eventStore = factory.Services.GetRequiredService<IEventStreamStore>();
        var documentStore = factory.Services.GetRequiredService<IDocumentStore>();
        var streamId = Guid.NewGuid();
        EventStoreProbeProjection.FailOnMarkedEvent = true;
        try
        {
            await eventStore.AppendAsync(streamId, 0, [CreateEvent(causeProjectionFailure: true)]);
            await Task.Delay(TimeSpan.FromMilliseconds(500));

            var committedEvents = await eventStore.ReadStreamAsync(streamId);
            Assert.Single(committedEvents);

            EventStoreProbeProjection.FailOnMarkedEvent = false;
            using var daemon = await documentStore.BuildProjectionDaemonAsync();
            await daemon.RebuildProjectionAsync("EventStoreProbe", CancellationToken.None);

            var recoveredState = await LoadProjectionAsync(documentStore, streamId);
            Assert.Equal(1, recoveredState.EventCount);
        }
        finally
        {
            EventStoreProbeProjection.FailOnMarkedEvent = false;
        }
    }

    private static DomainEvent CreateEvent(
        string? actorId = null,
        string? correlationId = null,
        string? causationId = null,
        bool causeProjectionFailure = false) =>
        new(
            Guid.NewGuid(),
            "character.registered",
            1,
            DateTimeOffset.UtcNow,
            JsonSerializer.SerializeToElement(new { displayName = "Test character", failProjection = causeProjectionFailure }),
            actorId,
            correlationId,
            causationId);

    private static async Task<Exception?> CaptureWriteAsync(
        IEventStreamStore store,
        Guid streamId,
        DomainEvent domainEvent)
    {
        try
        {
            await store.AppendAsync(streamId, 0, [domainEvent]);
            return null;
        }
        catch (StreamConcurrencyException exception)
        {
            return exception;
        }
    }

    private static async Task<EventStoreProbeState> LoadProjectionAsync(IDocumentStore documentStore, Guid streamId)
    {
        await using var session = documentStore.QuerySession();
        return await session.LoadAsync<EventStoreProbeState>(streamId) ??
               throw new InvalidOperationException("The asynchronous event projection has not produced a state.");
    }
}
