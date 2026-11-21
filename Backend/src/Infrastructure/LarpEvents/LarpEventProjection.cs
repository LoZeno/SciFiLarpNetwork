using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.LarpEventEvents;
using SciFiLarpNetwork.Backend.Domain.LarpEvents;
using SciFiLarpNetwork.Backend.Infrastructure.LarpEvents.LarpEventDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.LarpEvents;

public partial class LarpEventProjection : SingleStreamProjection<LarpEventDocument, Guid>
{
    public LarpEventProjection()
    {
        IncludeType<LarpEventCreated>();
        IncludeType<LarpEventDetailsChanged>();
        IncludeType<LarpEventDayAdded>();
        IncludeType<LarpEventDayRescheduled>();
        IncludeType<LarpEventDayRemoved>();
        IncludeType<LarpEventClosed>();
        IncludeType<LarpEventReopened>();
        IncludeType<LarpEventArchived>();
        IncludeType<LarpEventUnarchived>();
        IncludeType<ParticipantRegistered>();
        IncludeType<ParticipantUnregistered>();
        IncludeType<ParticipantExperienceChanged>();
    }

    public override LarpEventDocument? Evolve(LarpEventDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            LarpEventCreated created => new LarpEventDocument(
                created.EventId,
                created.Number,
                created.Name,
                created.NormalizedName,
                created.Description,
                LarpEventStatus.Open,
                IsArchived: false),
            LarpEventDetailsChanged changed when snapshot is not null => snapshot with
            {
                Name = changed.Name,
                NormalizedName = changed.NormalizedName,
                Description = changed.Description
            },
            LarpEventDayAdded added when snapshot is not null =>
                WithDays(snapshot, [.. snapshot.Days, new LarpEventDay(added.DayId, added.Date)]),
            LarpEventDayRescheduled rescheduled when snapshot is not null =>
                WithDays(snapshot, snapshot.Days.Select(day => day.DayId == rescheduled.DayId ? day with { Date = rescheduled.Date } : day)),
            LarpEventDayRemoved removed when snapshot is not null =>
                WithDays(snapshot, snapshot.Days.Where(day => day.DayId != removed.DayId)),
            LarpEventClosed when snapshot is not null => snapshot with { Status = LarpEventStatus.Closed },
            LarpEventReopened when snapshot is not null => snapshot with { Status = LarpEventStatus.Open },
            LarpEventArchived when snapshot is not null => snapshot with { IsArchived = true },
            LarpEventUnarchived when snapshot is not null => snapshot with { IsArchived = false },
            ParticipantRegistered registered when snapshot is not null => snapshot with
            {
                Participants = Register(snapshot.Participants, registered.CharacterId, registered.DayIds)
            },
            ParticipantUnregistered unregistered when snapshot is not null => snapshot with
            {
                Participants = Unregister(snapshot.Participants, unregistered.CharacterId, unregistered.DayIds)
            },
            ParticipantExperienceChanged experience when snapshot is not null => snapshot with
            {
                ExperienceChanges =
                [
                    .. snapshot.ExperienceChanges,
                    new LarpEventExperienceEntry(experience.CharacterId, experience.Amount, e.UserName, experience.OccurredAtUtc)
                ]
            },
            _ => snapshot
        };

    private static LarpEventDocument WithDays(LarpEventDocument snapshot, IEnumerable<LarpEventDay> days)
    {
        LarpEventDay[] ordered = [.. days.OrderBy(day => day.Date)];
        return snapshot with
        {
            Days = ordered,
            FirstDayNumber = ordered.Length == 0 ? null : ordered[0].Date.DayNumber
        };
    }

    private static LarpEventParticipant[] Register(
        LarpEventParticipant[] participants,
        Guid characterId,
        IReadOnlyList<Guid> dayIds)
    {
        var existing = participants.FirstOrDefault(participant => participant.CharacterId == characterId);
        return existing is null
            ? [.. participants, new LarpEventParticipant(characterId, [.. dayIds])]
            : [.. participants.Select(participant => participant == existing
                ? participant with { DayIds = [.. participant.DayIds.Union(dayIds)] }
                : participant)];
    }

    private static LarpEventParticipant[] Unregister(
        LarpEventParticipant[] participants,
        Guid characterId,
        IReadOnlyList<Guid> dayIds) =>
    [
        .. participants
            .Select(participant => participant.CharacterId == characterId
                ? participant with { DayIds = [.. participant.DayIds.Except(dayIds)] }
                : participant)
            .Where(participant => participant.DayIds.Length > 0)
    ];
}
