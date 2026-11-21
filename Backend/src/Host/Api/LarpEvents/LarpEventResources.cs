using SciFiLarpNetwork.Backend.Api.Players;
using SciFiLarpNetwork.Backend.Application.LarpEvents;
using SciFiLarpNetwork.Backend.Domain.LarpEvents;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.LarpEvents;

/// <summary>
/// Builds the Siren representations of events and their participants. Change actions are advertised only when
/// <paramref name="canWrite"/> is set and the event is Open (or, for status and archiving, when they apply); the
/// server still authorizes and validates every request. Printing changes nothing, so readers get it too.
/// </summary>
internal sealed class LarpEventResources(IApplicationText text, string pathBase, bool canWrite)
{
    private const string JsonType = "application/json";

    public string EventsPath => $"{pathBase}/api/v1/events";

    public string EventPath(Guid eventId) => $"{EventsPath}/{eventId:D}";

    public string ParticipantsPath(Guid eventId) => $"{EventPath(eventId)}/participants";

    private string CharactersPath => $"{pathBase}/api/v1/characters";

    public SirenEntity<LarpEventCollectionProperties> EventCollection(LarpEventPage page, LarpEventQuery query) =>
        new()
        {
            Class = ["event-collection"],
            Properties = new LarpEventCollectionProperties(
                text.GetLocalizedText("api.events"),
                text.GetLocalizedText("api.eventsDescription"),
                query.IncludeArchived,
                page.PageNumber,
                page.PageCount,
                page.TotalCount,
                Enum.GetValues<LarpEventStatus>()
                    .Select(status => new RosterOption(
                        SirenJson.ToElement(status).GetString()!,
                        text.GetLocalizedText(RosterResources.EnumTitleKey("events.status", status))))
                    .ToArray()),
            Entities = page.Items.Select(larpEvent => new SirenSubEntity
            {
                Rel = ["item"],
                Class = ["event"],
                Href = EventPath(larpEvent.EventId),
                Properties = SirenJson.ToElement(larpEvent)
            }).ToArray(),
            Links = PageLinks(page, query),
            Actions = canWrite
                ?
                [
                    new SirenAction
                    {
                        Name = "create-event",
                        Href = EventsPath,
                        Method = "POST",
                        Type = JsonType,
                        Title = text.GetLocalizedText("api.createEvent"),
                        Fields = DetailFields(null)
                    }
                ]
                : null
        };

    public SirenEntity<LarpEventSummary> Event(LarpEventSummary larpEvent)
    {
        var path = EventPath(larpEvent.EventId);
        return new SirenEntity<LarpEventSummary>
        {
            Class = ["event"],
            Properties = larpEvent,
            Links =
            [
                new SirenLink { Rel = ["self"], Href = path },
                new SirenLink { Rel = ["collection"], Href = EventsPath },
                new SirenLink { Rel = [SirenRelations.Participants], Href = ParticipantsPath(larpEvent.EventId) },
                new SirenLink { Rel = [SirenRelations.Characters], Href = CharactersPath }
            ],
            Actions = [ListParticipantsAction(larpEvent), PrintAction(larpEvent), .. canWrite ? EventActions(larpEvent) : []]
        };
    }

    private SirenAction ListParticipantsAction(LarpEventSummary larpEvent) =>
        new()
        {
            Name = "list-participants",
            Href = ParticipantsPath(larpEvent.EventId),
            Method = "GET",
            Title = text.GetLocalizedText("api.participants"),
            Fields = [Field("dayId", "hidden", "events.day", (Guid?)null)]
        };

    public SirenEntity<ParticipantCollectionProperties> ParticipantCollection(
        Guid eventId,
        Guid? dayId,
        IReadOnlyList<ParticipantSummary> participants) =>
        new()
        {
            Class = ["participant-collection"],
            Properties = new ParticipantCollectionProperties(
                text.GetLocalizedText("api.participants"),
                eventId,
                dayId,
                participants.Count),
            Entities = participants.Select(participant => new SirenSubEntity
            {
                Rel = ["item"],
                Class = ["participant"],
                Href = $"{CharactersPath}/{participant.CharacterId:D}",
                Properties = SirenJson.ToElement(participant)
            }).ToArray(),
            Links =
            [
                new SirenLink
                {
                    Rel = ["self"],
                    Href = dayId is { } day ? $"{ParticipantsPath(eventId)}?dayId={day:D}" : ParticipantsPath(eventId)
                },
                new SirenLink { Rel = ["up", SirenRelations.Event], Href = EventPath(eventId) }
            ]
        };

    private SirenAction PrintAction(LarpEventSummary larpEvent) =>
        new()
        {
            Name = "print-event-cards",
            Href = $"{EventPath(larpEvent.EventId)}/cards",
            Method = "POST",
            Type = JsonType,
            Title = text.GetLocalizedText("api.printEventCards"),
            Fields = [Field("dayId", "hidden", "events.day", (Guid?)null)]
        };

    private SirenAction[] EventActions(LarpEventSummary larpEvent)
    {
        var path = EventPath(larpEvent.EventId);
        if (larpEvent.Status == LarpEventStatus.Closed)
        {
            return larpEvent.IsArchived
                ? [SimpleAction("unarchive-event", $"{path}/unarchive", "api.unarchiveEvent")]
                :
                [
                    SimpleAction("reopen-event", $"{path}/reopen", "api.reopenEvent"),
                    SimpleAction("archive-event", $"{path}/archive", "api.archiveEvent")
                ];
        }

        var participants = ParticipantsPath(larpEvent.EventId);
        return
        [
            new SirenAction
            {
                Name = "update-event",
                Href = path,
                Method = "PUT",
                Type = JsonType,
                Title = text.GetLocalizedText("api.updateEvent"),
                Fields = [VersionField(larpEvent.Version), .. DetailFields(larpEvent)]
            },
            DayAction("add-day", $"{path}/days", "api.addDay", withDay: false, withDate: true),
            DayAction("reschedule-day", $"{path}/days/reschedule", "api.rescheduleDay", withDay: true, withDate: true),
            DayAction("remove-day", $"{path}/days/remove", "api.removeDay", withDay: true, withDate: false),
            new SirenAction
            {
                Name = "register-participant",
                Href = participants,
                Method = "POST",
                Type = JsonType,
                Title = text.GetLocalizedText("api.registerParticipant"),
                Fields = ParticipantDayFields()
            },
            new SirenAction
            {
                Name = "unregister-participant",
                Href = $"{participants}/unregister",
                Method = "POST",
                Type = JsonType,
                Title = text.GetLocalizedText("api.unregisterParticipant"),
                Fields = ParticipantDayFields()
            },
            new SirenAction
            {
                Name = "change-experience",
                Href = $"{participants}/experience",
                Method = "POST",
                Type = JsonType,
                Title = text.GetLocalizedText("api.changeExperience"),
                Fields =
                [
                    Field("characterId", "hidden", "events.character", (Guid?)null),
                    Field("amount", "number", "events.experienceChange", (int?)null)
                ]
            },
            SimpleAction("close-event", $"{path}/close", "api.closeEvent")
        ];
    }

    private SirenAction DayAction(string name, string href, string titleKey, bool withDay, bool withDate) =>
        new()
        {
            Name = name,
            Href = href,
            Method = "POST",
            Type = JsonType,
            Title = text.GetLocalizedText(titleKey),
            Fields =
            [
                .. withDay ? [Field("dayId", "hidden", "events.day", (Guid?)null)] : Array.Empty<SirenField>(),
                .. withDate ? [Field("date", "date", "events.date", (DateOnly?)null)] : Array.Empty<SirenField>()
            ]
        };

    private SirenField[] ParticipantDayFields() =>
    [
        Field("characterId", "hidden", "events.character", (Guid?)null),
        Field("dayIds", "hidden", "events.days", Array.Empty<Guid>())
    ];

    private SirenField[] DetailFields(LarpEventSummary? larpEvent) =>
    [
        Field("name", "text", "events.name", larpEvent?.Name),
        Field("description", "textarea", "events.description", larpEvent?.Description)
    ];

    private SirenLink[] PageLinks(LarpEventPage page, LarpEventQuery query)
    {
        var links = new List<SirenLink>
        {
            new() { Rel = ["self"], Href = PageHref(query) },
            new() { Rel = [SirenRelations.First], Href = PageHref(query with { PageNumber = 1 }) },
            new() { Rel = [SirenRelations.Last], Href = PageHref(query with { PageNumber = page.PageCount }) },
            new() { Rel = [SirenRelations.WithArchived], Href = PageHref(query with { IncludeArchived = true, PageNumber = 1 }) },
            new() { Rel = [SirenRelations.WithoutArchived], Href = PageHref(query with { IncludeArchived = false, PageNumber = 1 }) },
            new() { Rel = [SirenRelations.Characters], Href = CharactersPath }
        };
        if (page.PageNumber > 1)
        {
            links.Add(new SirenLink
            {
                Rel = [SirenRelations.Previous],
                Href = PageHref(query with { PageNumber = Math.Min(page.PageNumber - 1, page.PageCount) })
            });
        }

        if (page.PageNumber < page.PageCount)
        {
            links.Add(new SirenLink { Rel = [SirenRelations.Next], Href = PageHref(query with { PageNumber = page.PageNumber + 1 }) });
        }

        return [.. links];
    }

    private string PageHref(LarpEventQuery query)
    {
        var parameters = new List<string>();
        if (query.IncludeArchived)
        {
            parameters.Add("includeArchived=true");
        }

        if (query.PageNumber > 1)
        {
            parameters.Add($"page={query.PageNumber}");
        }

        return parameters.Count == 0 ? EventsPath : $"{EventsPath}?{string.Join('&', parameters)}";
    }

    private SirenAction SimpleAction(string name, string href, string titleKey) =>
        new() { Name = name, Href = href, Method = "POST", Type = JsonType, Title = text.GetLocalizedText(titleKey) };

    private SirenField VersionField(long version) =>
        new() { Name = "version", Type = "hidden", Value = SirenJson.ToElement(version), Title = text.GetLocalizedText("skills.version") };

    private SirenField Field<T>(string name, string type, string titleKey, T? value) =>
        new()
        {
            Name = name,
            Type = type,
            Title = text.GetLocalizedText(titleKey),
            Value = value is null ? null : SirenJson.ToElement(value)
        };
}
