using Marten;
using Marten.Exceptions;
using Marten.Pagination;
using SciFiLarpNetwork.Backend.Application.Messages;
using SciFiLarpNetwork.Backend.Domain.Events.MessageEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Messages.MessageDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;
using SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Messages;

/// <summary>
/// A message is its own stream, started with the identifier the client chose, so a repeated submission collides with
/// the message already sent instead of sending it twice. The sender and recipients are only read: a character
/// archived at the moment of sending still gets the message, as it would had it been archived a moment later.
/// </summary>
internal sealed class MessageService(AttributedSessionFactory sessions, TimeProvider timeProvider) : IMessageService
{
    public async Task<MessagePage> GetMessagesAsync(MessageQuery query, CancellationToken cancellationToken = default)
    {
        await using var session = sessions.QuerySession();
        IQueryable<MessageDocument> messages = session.Query<MessageDocument>();
        if (query.CharacterId is { } characterId)
        {
            messages = messages.Where(message => message.RecipientIds.Contains(characterId));
        }

        var page = await messages
            .OrderByDescending(message => message.SentAtUtc)
            .ThenByDescending(message => message.Id)
            .ToPagedListAsync(query.PageNumber, MessageQuery.PageSize, cancellationToken);
        var characters = await CharactersAsync(session, page.Select(message => message.SenderId), cancellationToken);
        return new MessagePage(
            page.Select(message => new MessageSummary(
                    message.Id,
                    characters.Describe(message.SenderId),
                    message.RecipientIds.Length,
                    message.Title,
                    message.SentAtUtc,
                    message.ReplyToMessageId))
                .ToArray(),
            (int)page.PageNumber,
            Math.Max(1, (int)page.PageCount),
            page.TotalItemCount);
    }

    public async Task<MessageResult<MessageDetails>> OpenMessageAsync(Guid messageId,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        if (await session.LoadAsync<MessageDocument>(messageId, cancellationToken) is not { } message)
        {
            return Failure(MessageOutcome.NotFound);
        }

        // The opening is history of its own: nothing decides on it, so it is appended without a version check.
        session.Events.Append(messageId, new MessageOpened(messageId, timeProvider.GetUtcNow()));
        await session.SaveChangesAsync(cancellationToken);
        return Success(await ToDetailsAsync(session, message, cancellationToken));
    }

    public async Task<MessageResult<MessageDetails>> SendMessageAsync(MessageDraft draft,
        CancellationToken cancellationToken = default)
    {
        if (MessageDraftValidator.Validate(draft) is { } invalid)
        {
            return Failure(invalid);
        }

        await using var session = sessions.OpenSession();
        if (await session.LoadAsync<MessageDocument>(draft.MessageId, cancellationToken) is { } existing)
        {
            return await RepeatedAsync(session, existing, draft, cancellationToken);
        }

        if (await CheckAsync(session, draft, cancellationToken) is { Outcome: not MessageOutcome.Succeeded } rejected)
        {
            return rejected;
        }

        var sent = new MessageSent(
            draft.MessageId,
            draft.SenderId,
            draft.DistinctRecipientIds,
            draft.Title!.Trim(),
            draft.Body!,
            draft.ReplyToMessageId,
            timeProvider.GetUtcNow());
        session.Events.StartStream<MessageDocument>(draft.MessageId, sent);
        try
        {
            await session.SaveChangesAsync(cancellationToken);
        }
        catch (ExistingStreamIdCollisionException)
        {
            // The same submission committed first, or the identifier names another stream.
            await using var query = sessions.QuerySession();
            return await query.LoadAsync<MessageDocument>(draft.MessageId, cancellationToken) is { } committed
                ? await RepeatedAsync(query, committed, draft, cancellationToken)
                : Failure(MessageOutcome.MessageIdInUse);
        }

        var saved = await session.LoadAsync<MessageDocument>(draft.MessageId, cancellationToken);
        return Success(await ToDetailsAsync(session, saved!, cancellationToken));
    }

    /// <summary>
    /// Checks the stored data a new message refers to. A repeated submission skips this, so it still returns the
    /// message when a character was archived after the first one was sent.
    /// </summary>
    private static async Task<MessageResult<MessageDetails>> CheckAsync(
        IQuerySession session,
        MessageDraft draft,
        CancellationToken cancellationToken)
    {
        if (draft.ReplyToMessageId is { } source &&
            await session.LoadAsync<MessageDocument>(source, cancellationToken) is null)
        {
            return Failure(MessageOutcome.UnknownReplySource);
        }

        Guid[] characterIds = [.. new[] { draft.SenderId }.Concat(draft.DistinctRecipientIds).Distinct()];
        var characters = (await session.LoadManyAsync<CharacterDocument>(cancellationToken, characterIds))
            .ToDictionary(character => character.Id);
        Guid[] unknown = [.. characterIds.Where(id => !characters.ContainsKey(id))];
        if (unknown.Length > 0)
        {
            return Failure(MessageOutcome.UnknownCharacters, unknown);
        }

        Guid[] archived = [.. characterIds.Where(id => characters[id].IsArchived)];
        return archived.Length > 0
            ? Failure(MessageOutcome.ArchivedCharacters, archived)
            : new MessageResult<MessageDetails>(MessageOutcome.Succeeded, null, []);
    }

    private static async Task<MessageResult<MessageDetails>> RepeatedAsync(
        IQuerySession session,
        MessageDocument existing,
        MessageDraft draft,
        CancellationToken cancellationToken) =>
        existing.SenderId == draft.SenderId &&
        existing.RecipientIds.SequenceEqual(draft.DistinctRecipientIds) &&
        existing.Title == draft.Title!.Trim() &&
        existing.Body == draft.Body &&
        existing.ReplyToMessageId == draft.ReplyToMessageId
            ? Success(await ToDetailsAsync(session, existing, cancellationToken))
            : Failure(MessageOutcome.MessageIdInUse);

    private static async Task<MessageDetails> ToDetailsAsync(
        IQuerySession session,
        MessageDocument message,
        CancellationToken cancellationToken)
    {
        var characters = await CharactersAsync(session, [message.SenderId, .. message.RecipientIds], cancellationToken);
        var source = message.ReplyToMessageId is { } sourceId
            ? await session.LoadAsync<MessageDocument>(sourceId, cancellationToken)
            : null;
        return new MessageDetails(
            message.Id,
            characters.Describe(message.SenderId),
            message.RecipientIds.Select(characters.Describe).ToArray(),
            message.Title,
            message.Body,
            message.SentAtUtc,
            await AdministratorNameAsync(session, message.SentBy, cancellationToken),
            source is null ? null : new MessageReference(source.Id, source.Title));
    }

    private static async Task<MessageCharacters> CharactersAsync(
        IQuerySession session,
        IEnumerable<Guid> characterIds,
        CancellationToken cancellationToken)
    {
        Guid[] ids = [.. characterIds.Distinct()];
        if (ids.Length == 0)
        {
            return new MessageCharacters(new Dictionary<Guid, CharacterDocument>());
        }

        return new MessageCharacters((await session.LoadManyAsync<CharacterDocument>(cancellationToken, ids))
            .ToDictionary(character => character.Id));
    }

    /// <summary>
    /// The user name of the administrator account recorded as the sender; a recorded value that is not a known account
    /// is shown as recorded.
    /// </summary>
    private static async Task<string?> AdministratorNameAsync(
        IQuerySession session,
        string? recorded,
        CancellationToken cancellationToken) =>
        recorded is not null && Guid.TryParse(recorded, out var accountId) &&
        await session.LoadAsync<AdministratorAccountDocument>(accountId, cancellationToken) is { } account
            ? account.UserName
            : recorded;

    private static MessageResult<MessageDetails> Success(MessageDetails details) =>
        new(MessageOutcome.Succeeded, details, []);

    private static MessageResult<MessageDetails> Failure(MessageOutcome outcome, IReadOnlyList<Guid>? characterIds = null) =>
        new(outcome, null, characterIds ?? []);

    private sealed record MessageCharacters(IReadOnlyDictionary<Guid, CharacterDocument> Characters)
    {
        public MessageCharacter Describe(Guid characterId) =>
            Characters.TryGetValue(characterId, out var character)
                ? new MessageCharacter(characterId, true, character.Number, character.Name, character.IsArchived)
                : new MessageCharacter(characterId, false, null, null, false);
    }
}
