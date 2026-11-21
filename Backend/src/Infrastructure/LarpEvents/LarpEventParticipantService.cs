using Marten;
using SciFiLarpNetwork.Backend.Application.LarpEvents;
using SciFiLarpNetwork.Backend.Domain.Characters;
using SciFiLarpNetwork.Backend.Domain.Events.CharacterEvents;
using SciFiLarpNetwork.Backend.Domain.Events.LarpEventEvents;
using SciFiLarpNetwork.Backend.Domain.LarpEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.LarpEvents.LarpEventDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;
using SciFiLarpNetwork.Backend.Infrastructure.Players;
using SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.LarpEvents;

/// <summary>
/// Registrations take the event stream for writing. The character is only read: a character whose status changes at
/// the moment of registration stays registered, which the specification allows for later status changes. An
/// experience change also takes the character stream for writing, because it changes the character's experience
/// points; a character edit at the same moment conflicts with it.
/// </summary>
internal sealed class LarpEventParticipantService(AttributedSessionFactory sessions, TimeProvider timeProvider)
    : ILarpEventParticipantService
{
    public async Task<LarpEventResult<IReadOnlyList<ParticipantSummary>>> GetParticipantsAsync(
        Guid eventId,
        Guid? dayId,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.QuerySession();
        if (await session.LoadAsync<LarpEventDocument>(eventId, cancellationToken) is not { } larpEvent)
        {
            return LarpEventResult<IReadOnlyList<ParticipantSummary>>.Failure(LarpEventOutcome.NotFound);
        }

        if (dayId is { } selected && !larpEvent.HasDay(selected))
        {
            return LarpEventResult<IReadOnlyList<ParticipantSummary>>.Failure(LarpEventOutcome.UnknownDay);
        }

        var participants = larpEvent.Participants
            .Where(participant => dayId is not { } day || participant.DayIds.Contains(day))
            .ToArray();
        return LarpEventResult<IReadOnlyList<ParticipantSummary>>.Success(
            await ToSummariesAsync(session, larpEvent, participants, cancellationToken));
    }

    public async Task<LarpEventResult<ParticipantSummary>> RegisterAsync(
        Guid eventId,
        Guid characterId,
        IReadOnlyList<Guid> dayIds,
        CancellationToken cancellationToken = default)
    {
        var requested = dayIds.Distinct().ToArray();
        if (requested.Length == 0)
        {
            return LarpEventResult<ParticipantSummary>.Failure(LarpEventOutcome.NoDaysSelected);
        }

        await using var session = sessions.OpenSession();
        var (outcome, stream) = await session.FetchOpenEventAsync(eventId, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return LarpEventResult<ParticipantSummary>.Failure(outcome);
        }

        if (!requested.All(day => current.HasDay(day)))
        {
            return LarpEventResult<ParticipantSummary>.Failure(LarpEventOutcome.UnknownDay);
        }

        switch (await session.LoadAsync<CharacterDocument>(characterId, cancellationToken))
        {
            case null:
                return LarpEventResult<ParticipantSummary>.Failure(LarpEventOutcome.UnknownCharacter);
            case { Status: not CharacterStatus.Active }:
                return LarpEventResult<ParticipantSummary>.Failure(LarpEventOutcome.CharacterNotActive);
        }

        var registered = current.Participant(characterId)?.DayIds ?? [];
        var newDays = requested.Except(registered).ToArray();
        if (newDays.Length == 0)
        {
            return LarpEventResult<ParticipantSummary>.Failure(LarpEventOutcome.AlreadyRegistered);
        }

        stream.AppendOne(new ParticipantRegistered(eventId, characterId, newDays, timeProvider.GetUtcNow()));
        return await SaveParticipantAsync(session, eventId, characterId, cancellationToken);
    }

    public async Task<LarpEventResult<LarpEventSummary>> UnregisterAsync(
        Guid eventId,
        Guid characterId,
        IReadOnlyList<Guid> dayIds,
        CancellationToken cancellationToken = default)
    {
        var requested = dayIds.Distinct().ToArray();
        if (requested.Length == 0)
        {
            return LarpEventResult<LarpEventSummary>.Failure(LarpEventOutcome.NoDaysSelected);
        }

        await using var session = sessions.OpenSession();
        var (outcome, stream) = await session.FetchOpenEventAsync(eventId, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return LarpEventResult<LarpEventSummary>.Failure(outcome);
        }

        if (!requested.All(day => current.HasDay(day)))
        {
            return LarpEventResult<LarpEventSummary>.Failure(LarpEventOutcome.UnknownDay);
        }

        if (current.Participant(characterId) is not { } participant || requested.Except(participant.DayIds).Any())
        {
            return LarpEventResult<LarpEventSummary>.Failure(LarpEventOutcome.NotRegistered);
        }

        stream.AppendOne(new ParticipantUnregistered(eventId, characterId, requested, timeProvider.GetUtcNow()));
        var committed = await session.CommitEventAsync(LarpEventWrites.ConcurrentChange, cancellationToken);
        if (committed != LarpEventOutcome.Succeeded)
        {
            return LarpEventResult<LarpEventSummary>.Failure(committed);
        }

        var saved = await session.LoadAsync<LarpEventDocument>(eventId, cancellationToken);
        return LarpEventResult<LarpEventSummary>.Success(saved!.ToSummary());
    }

    public async Task<LarpEventResult<ParticipantSummary>> ChangeExperienceAsync(
        Guid eventId,
        Guid characterId,
        int amount,
        CancellationToken cancellationToken = default)
    {
        if (!ExperienceChanges.IsValidAmount(amount))
        {
            return LarpEventResult<ParticipantSummary>.Failure(LarpEventOutcome.InvalidExperienceChange);
        }

        await using var session = sessions.OpenSession();
        var (outcome, stream) = await session.FetchOpenEventAsync(eventId, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return LarpEventResult<ParticipantSummary>.Failure(outcome);
        }

        if (current.Participant(characterId) is null)
        {
            return LarpEventResult<ParticipantSummary>.Failure(LarpEventOutcome.NotRegistered);
        }

        var characterStream = await session.Events.FetchForWriting<CharacterDocument>(characterId, cancellationToken);
        if (characterStream.Aggregate is not { } character)
        {
            return LarpEventResult<ParticipantSummary>.Failure(LarpEventOutcome.UnknownCharacter);
        }

        if (ExperienceChanges.Apply(character.ExperiencePoints, amount) is not { } experiencePoints)
        {
            return LarpEventResult<ParticipantSummary>.Failure(LarpEventOutcome.ExperienceOutOfRange);
        }

        if (await CharacterFreePoints.LeavesNegativeAsync(session, character, experiencePoints, cancellationToken))
        {
            return LarpEventResult<ParticipantSummary>.Failure(LarpEventOutcome.InsufficientFreePoints);
        }

        var now = timeProvider.GetUtcNow();
        stream.AppendOne(new ParticipantExperienceChanged(eventId, characterId, amount, now));
        characterStream.AppendOne(new CharacterExperienceChanged(characterId, eventId, amount, experiencePoints, now));
        return await SaveParticipantAsync(session, eventId, characterId, cancellationToken);
    }

    private static async Task<LarpEventResult<ParticipantSummary>> SaveParticipantAsync(
        IDocumentSession session,
        Guid eventId,
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var outcome = await session.CommitEventAsync(LarpEventWrites.ConcurrentChange, cancellationToken);
        if (outcome != LarpEventOutcome.Succeeded)
        {
            return LarpEventResult<ParticipantSummary>.Failure(outcome);
        }

        var saved = await session.LoadAsync<LarpEventDocument>(eventId, cancellationToken);
        var summaries = await ToSummariesAsync(session, saved!, [saved!.Participant(characterId)!], cancellationToken);
        return LarpEventResult<ParticipantSummary>.Success(summaries[0]);
    }

    private static async Task<IReadOnlyList<ParticipantSummary>> ToSummariesAsync(
        IQuerySession session,
        LarpEventDocument larpEvent,
        LarpEventParticipant[] participants,
        CancellationToken cancellationToken)
    {
        if (participants.Length == 0)
        {
            return [];
        }

        var characters = (await session.LoadManyAsync<CharacterDocument>(
                cancellationToken,
                participants.Select(participant => participant.CharacterId).ToArray()))
            .ToDictionary(character => character.Id);
        var playerNames = (await session.LoadManyAsync<PlayerDocument>(
                cancellationToken,
                characters.Values.Select(character => character.PlayerId).Distinct().ToArray()))
            .ToDictionary(player => player.Id, player => player.DisplayName);
        var administratorNames = await AdministratorNamesAsync(session, larpEvent, cancellationToken);
        return participants
            .Where(participant => characters.ContainsKey(participant.CharacterId))
            .Select(participant => larpEvent.ToParticipantSummary(
                participant, characters[participant.CharacterId], playerNames, administratorNames))
            .OrderBy(participant => participant.CharacterNumber)
            .ToArray();
    }

    /// <summary>
    /// The user names of the administrator accounts that gave experience changes. Events record the account
    /// identifier; a recorded value that is not a known account is shown as recorded.
    /// </summary>
    private static async Task<IReadOnlyDictionary<string, string>> AdministratorNamesAsync(
        IQuerySession session,
        LarpEventDocument larpEvent,
        CancellationToken cancellationToken)
    {
        var accountIds = larpEvent.ExperienceChanges
            .Select(change => Guid.TryParse(change.ChangedBy, out var accountId) ? accountId : (Guid?)null)
            .OfType<Guid>()
            .Distinct()
            .ToArray();
        if (accountIds.Length == 0)
        {
            return new Dictionary<string, string>();
        }

        return (await session.LoadManyAsync<AdministratorAccountDocument>(cancellationToken, accountIds))
            .ToDictionary(account => account.Id.ToString("D"), account => account.UserName, StringComparer.OrdinalIgnoreCase);
    }
}
