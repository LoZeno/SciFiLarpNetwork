namespace SciFiLarpNetwork.Backend.Domain.LarpEvents;

/// <summary>Whether an event can still change. A Closed event keeps its details, days, registrations, and experience.</summary>
public enum LarpEventStatus
{
    Open,
    Closed
}
