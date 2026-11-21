namespace SciFiLarpNetwork.Backend.Api.LarpEvents;

/// <param name="DayId">The day the list is for; <see langword="null"/> for the whole event.</param>
public sealed record ParticipantCollectionProperties(string Title, Guid EventId, Guid? DayId, int Count);
