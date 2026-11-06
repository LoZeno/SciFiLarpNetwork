using System.Text.Json;
using JasperFx.Events;
using Marten;
using Marten.Events;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.EventSourcing;
using SciFiLarpNetwork.Backend.Domain.Events;
using SciFiLarpNetwork.Backend.Tests.Integration.Api.Fixtures;

namespace SciFiLarpNetwork.Backend.Tests.Integration.EventSourcing;

[Collection("Backend database integration")]
public sealed class EventStoreIntegrationTests(ApiApplicationFactory factory) : IClassFixture<ApiApplicationFactory>
{
    [Fact]
    public async Task AppendResultContainsStreamVersionEventIdsAndDuplicateStatus()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();
        var firstEvent = CreateEvent(actorId: "admin-1", correlationId: "correlation-1");
        var secondEvent = CreateEvent(actorId: "admin-1", correlationId: "correlation-1", causationId: "cause-1");

        var append = await store.AppendAsync(streamId, 0, [firstEvent, secondEvent], cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(
            new AppendResultSnapshot(2, false, $"{firstEvent.EventId:D}|{secondEvent.EventId:D}"),
            Snapshot(append));
    }

    [Fact]
    public async Task AppendAndReadPreservesOrderedEventsAndMetadata()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();
        var firstEvent = CreateEvent(actorId: "admin-1", correlationId: "correlation-1");
        var secondEvent = CreateEvent(actorId: "admin-1", correlationId: "correlation-1", causationId: "cause-1");
        await store.AppendAsync(streamId, 0, [firstEvent, secondEvent], cancellationToken: TestContext.Current.CancellationToken);

        var events = await store.ReadStreamAsync(streamId, TestContext.Current.CancellationToken);

        Assert.Equal(
            new[]
            {
                new StoredEventSnapshot(
                    firstEvent.EventId,
                    streamId,
                    1,
                    "character.registered",
                    1,
                    firstEvent.OccurredAtUtc.Offset,
                    TimeSpan.Zero,
                    "admin-1",
                    "correlation-1",
                    null,
                    "Test character"),
                new StoredEventSnapshot(
                    secondEvent.EventId,
                    streamId,
                    2,
                    "character.registered",
                    1,
                    secondEvent.OccurredAtUtc.Offset,
                    TimeSpan.Zero,
                    "admin-1",
                    "correlation-1",
                    "cause-1",
                    "Test character")
            },
            events.Select(Snapshot).ToArray());
    }

    [Fact]
    public async Task MartenEventsPreserveActorCorrelationAndCausationMetadata()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var documentStore = factory.Services.GetRequiredService<IDocumentStore>();
        var streamId = Guid.NewGuid();
        await store.AppendAsync(
            streamId,
            0,
            [
                CreateEvent(actorId: "admin-1", correlationId: "correlation-1"),
                CreateEvent(actorId: "admin-1", correlationId: "correlation-1", causationId: "cause-1")
            ],
            cancellationToken: TestContext.Current.CancellationToken);

        await using var session = documentStore.QuerySession();
        var providerEvents = await session.Events.FetchStreamAsync(streamId, token: TestContext.Current.CancellationToken);

        Assert.Equal(
            new[]
            {
                new ProviderEventMetadata("admin-1", "correlation-1", null),
                new ProviderEventMetadata("admin-1", "correlation-1", "cause-1")
            },
            providerEvents.Select(domainEvent =>
                new ProviderEventMetadata(domainEvent.UserName, domainEvent.CorrelationId, domainEvent.CausationId))
                .ToArray());
    }

    [Fact]
    public async Task StaleExpectedVersionRejectsAppendWithoutChangingTheStream()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();
        var originalEvent = CreateEvent();
        await store.AppendAsync(streamId, 0, [originalEvent], cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<StreamConcurrencyException>(
            () => store.AppendAsync(streamId, 0, [CreateEvent()], cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StaleExpectedVersionFailureLeavesTheStreamUnchanged()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();
        var originalEvent = CreateEvent();
        await store.AppendAsync(streamId, 0, [originalEvent], cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Record.ExceptionAsync(() => store.AppendAsync(streamId, 0, [CreateEvent()], cancellationToken: TestContext.Current.CancellationToken));
        var events = await store.ReadStreamAsync(streamId, TestContext.Current.CancellationToken);

        Assert.Equal(
            new { ExceptionType = typeof(StreamConcurrencyException), EventIds = originalEvent.EventId.ToString("D") },
            new
            {
                ExceptionType = exception?.GetType() ?? typeof(object),
                EventIds = string.Join('|', events.Select(domainEvent => domainEvent.EventId.ToString("D")))
            });
    }

    [Fact]
    public async Task AppendAtIncorrectNonzeroVersionIsRejected()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();
        await store.AppendAsync(streamId, 0, [CreateEvent()], cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<StreamConcurrencyException>(
            () => store.AppendAsync(streamId, 2, [CreateEvent()], cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AppendAtIncorrectNonzeroVersionLeavesStreamUnchanged()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();
        var originalEvent = CreateEvent();
        await store.AppendAsync(streamId, 0, [originalEvent], cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Record.ExceptionAsync(() => store.AppendAsync(streamId, 2, [CreateEvent()], cancellationToken: TestContext.Current.CancellationToken));
        var events = await store.ReadStreamAsync(streamId, TestContext.Current.CancellationToken);

        Assert.Equal(
            new { ExceptionType = typeof(StreamConcurrencyException), EventIds = originalEvent.EventId.ToString("D") },
            new
            {
                ExceptionType = exception?.GetType() ?? typeof(object),
                EventIds = string.Join('|', events.Select(domainEvent => domainEvent.EventId.ToString("D")))
            });
    }

    [Fact]
    public async Task SimultaneousWritesFromOneVersionCommitOnlyOneBatch()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();

        var writes = await Task.WhenAll(
            CaptureWriteAsync(store, streamId, CreateEvent()),
            CaptureWriteAsync(store, streamId, CreateEvent()));

        var committedEvents = await store.ReadStreamAsync(streamId, TestContext.Current.CancellationToken);

        Assert.Equal(
            new WriteOutcomeSnapshot(1, 1, 1),
            new WriteOutcomeSnapshot(
                writes.Count(result => result is null),
                writes.Count(result => result is StreamConcurrencyException),
                committedEvents.Count));
    }

    [Fact]
    public async Task CommandIdCreatesANonDuplicateResultForTheFirstAppend()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();
        var commandId = Guid.NewGuid();
        var domainEvent = CreateEvent();

        var firstResult = await store.AppendAsync(streamId, 0, [domainEvent], commandId, TestContext.Current.CancellationToken);

        Assert.Equal(
            new AppendResultSnapshot(1, false, domainEvent.EventId.ToString("D")),
            Snapshot(firstResult));
    }

    [Fact]
    public async Task CommandIdMakesSequentialRetriesIdempotent()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();
        var commandId = Guid.NewGuid();
        var domainEvent = CreateEvent();

        await store.AppendAsync(streamId, 0, [domainEvent], commandId, TestContext.Current.CancellationToken);
        var retryResult = await store.AppendAsync(streamId, 0, [domainEvent], commandId, TestContext.Current.CancellationToken);
        var events = await store.ReadStreamAsync(streamId, TestContext.Current.CancellationToken);

        Assert.Equal(
            new { Result = new AppendResultSnapshot(1, true, domainEvent.EventId.ToString("D")), EventCount = 1 },
            new { Result = Snapshot(retryResult), EventCount = events.Count });
    }

    [Fact]
    public async Task CommandIdCannotBeReusedForDifferentIntent()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();
        var commandId = Guid.NewGuid();
        await store.AppendAsync(streamId, 0, [CreateEvent()], commandId, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<IdempotencyKeyReuseException>(
            () => store.AppendAsync(streamId, 1, [CreateEvent()], commandId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReusingCommandIdForDifferentIntentLeavesTheStreamUnchanged()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();
        var commandId = Guid.NewGuid();
        var originalEvent = CreateEvent();
        await store.AppendAsync(streamId, 0, [originalEvent], commandId, TestContext.Current.CancellationToken);

        var exception = await Record.ExceptionAsync(
            () => store.AppendAsync(streamId, 1, [CreateEvent()], commandId, TestContext.Current.CancellationToken));
        var events = await store.ReadStreamAsync(streamId, TestContext.Current.CancellationToken);

        Assert.Equal(
            new { ExceptionType = typeof(IdempotencyKeyReuseException), EventIds = originalEvent.EventId.ToString("D") },
            new
            {
                ExceptionType = exception?.GetType() ?? typeof(object),
                EventIds = string.Join('|', events.Select(domainEvent => domainEvent.EventId.ToString("D")))
            });
    }

    [Fact]
    public async Task EventIdsAreGloballyUniqueAndFailedBatchesAreAtomic()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var originalStreamId = Guid.NewGuid();
        var targetStreamId = Guid.NewGuid();
        var originalEvent = CreateEvent();
        await store.AppendAsync(originalStreamId, 0, [originalEvent], cancellationToken: TestContext.Current.CancellationToken);

        var newEvent = CreateEvent();
        var duplicateEvent = new DomainEvent(
            originalEvent.EventId,
            originalEvent.EventType,
            originalEvent.SchemaVersion,
            originalEvent.OccurredAtUtc,
            originalEvent.Payload);
        await Assert.ThrowsAsync<JasperFx.DocumentAlreadyExistsException>(
            () => store.AppendAsync(targetStreamId, 0, [newEvent, duplicateEvent], cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DuplicateEventIdFailureLeavesBothStreamsUnchanged()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var originalStreamId = Guid.NewGuid();
        var targetStreamId = Guid.NewGuid();
        var originalEvent = CreateEvent();
        await store.AppendAsync(originalStreamId, 0, [originalEvent], cancellationToken: TestContext.Current.CancellationToken);
        var newEvent = CreateEvent();
        var duplicateEvent = new DomainEvent(
            originalEvent.EventId,
            originalEvent.EventType,
            originalEvent.SchemaVersion,
            originalEvent.OccurredAtUtc,
            originalEvent.Payload);

        var exception = await Record.ExceptionAsync(
            () => store.AppendAsync(targetStreamId, 0, [newEvent, duplicateEvent], cancellationToken: TestContext.Current.CancellationToken));
        var targetEvents = await store.ReadStreamAsync(targetStreamId, TestContext.Current.CancellationToken);
        var originalEvents = await store.ReadStreamAsync(originalStreamId, TestContext.Current.CancellationToken);

        Assert.Equal(
            new { ExceptionType = typeof(JasperFx.DocumentAlreadyExistsException), TargetCount = 0, OriginalCount = 1 },
            new
            {
                ExceptionType = exception?.GetType() ?? typeof(object),
                TargetCount = targetEvents.Count,
                OriginalCount = originalEvents.Count
            });
    }

    [Fact]
    public async Task EmptyStreamReadsAsEmpty()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();

        Assert.Empty(await store.ReadStreamAsync(streamId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task WriteWithNoEventsIsRejected()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentException>(() => store.AppendAsync(streamId, 0, [], cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RejectedEmptyWriteDoesNotCreateAStream()
    {
        var store = factory.Services.GetRequiredService<IEventStreamStore>();
        var streamId = Guid.NewGuid();

        var exception = await Record.ExceptionAsync(() => store.AppendAsync(streamId, 0, [], cancellationToken: TestContext.Current.CancellationToken));
        var events = await store.ReadStreamAsync(streamId, TestContext.Current.CancellationToken);

        Assert.Equal(
            new { ExceptionType = typeof(ArgumentException), EventCount = 0 },
            new { ExceptionType = exception?.GetType() ?? typeof(object), EventCount = events.Count });
    }

    [Fact]
    public async Task AsynchronousProjectionCanBeRebuiltFromCommittedEvents()
    {
        var eventStore = factory.Services.GetRequiredService<IEventStreamStore>();
        var documentStore = factory.Services.GetRequiredService<IDocumentStore>();
        var streamId = Guid.NewGuid();
        await eventStore.AppendAsync(streamId, 0, [CreateEvent(), CreateEvent()], cancellationToken: TestContext.Current.CancellationToken);

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
        var eventStore = factory.Services.GetRequiredService<IEventStreamStore>();
        var documentStore = factory.Services.GetRequiredService<IDocumentStore>();
        var streamId = Guid.NewGuid();
        EventStoreProbeProjection.FailOnMarkedEvent = true;
        try
        {
            await eventStore.AppendAsync(streamId, 0, [CreateEvent(causeProjectionFailure: true)], cancellationToken: TestContext.Current.CancellationToken);
            await Task.Delay(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken);

            var committedEvents = await eventStore.ReadStreamAsync(streamId, TestContext.Current.CancellationToken);

            EventStoreProbeProjection.FailOnMarkedEvent = false;
            using var daemon = await documentStore.BuildProjectionDaemonAsync();
            await daemon.RebuildProjectionAsync("EventStoreProbe", CancellationToken.None);

            var recoveredState = await LoadProjectionAsync(documentStore, streamId);

            Assert.Equal(
                new ProjectionRecoverySnapshot(1, new EventStoreProbeState(streamId, 1, 1)),
                new ProjectionRecoverySnapshot(committedEvents.Count, recoveredState));
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

    private static AppendResultSnapshot Snapshot(AppendResult result) =>
        new(result.StreamVersion, result.WasDuplicate, string.Join('|', result.EventIds.Select(eventId => eventId.ToString("D"))));

    private static StoredEventSnapshot Snapshot(StoredDomainEvent domainEvent) =>
        new(
            domainEvent.EventId,
            domainEvent.StreamId,
            domainEvent.Sequence,
            domainEvent.EventType,
            domainEvent.SchemaVersion,
            domainEvent.OccurredAtUtc.Offset,
            domainEvent.RecordedAtUtc.Offset,
            domainEvent.ActorId,
            domainEvent.CorrelationId,
            domainEvent.CausationId,
            domainEvent.Payload.GetProperty("displayName").GetString()!);

    private sealed record AppendResultSnapshot(long StreamVersion, bool WasDuplicate, string EventIds);

    private sealed record StoredEventSnapshot(
        Guid EventId,
        Guid StreamId,
        long Sequence,
        string EventType,
        int SchemaVersion,
        TimeSpan OccurredAtOffset,
        TimeSpan RecordedAtOffset,
        string? ActorId,
        string? CorrelationId,
        string? CausationId,
        string DisplayName);

    private sealed record ProviderEventMetadata(string? UserName, string? CorrelationId, string? CausationId);

    private sealed record WriteOutcomeSnapshot(int CommittedWrites, int ConcurrencyFailures, int EventCount);

    private sealed record ProjectionRecoverySnapshot(int CommittedEvents, EventStoreProbeState RecoveredState);

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
