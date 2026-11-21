namespace SciFiLarpNetwork.Backend.Application.LarpEvents;

/// <summary>The characters registered for an event's days, and the experience changes given to them.</summary>
public interface ILarpEventParticipantService
{
    /// <summary>
    /// Lists the characters registered for <paramref name="dayId"/>, or for any day when it is
    /// <see langword="null"/>, in character running-number order.
    /// </summary>
    Task<LarpEventResult<IReadOnlyList<ParticipantSummary>>> GetParticipantsAsync(
        Guid eventId,
        Guid? dayId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an Active character for the requested days of an Open event. Days the character is already
    /// registered for are left as they are; asking only for such days reports
    /// <see cref="LarpEventOutcome.AlreadyRegistered"/>.
    /// </summary>
    Task<LarpEventResult<ParticipantSummary>> RegisterAsync(
        Guid eventId,
        Guid characterId,
        IReadOnlyList<Guid> dayIds,
        CancellationToken cancellationToken = default);

    /// <summary>Unregisters a character from the requested days of an Open event.</summary>
    Task<LarpEventResult<LarpEventSummary>> UnregisterAsync(
        Guid eventId,
        Guid characterId,
        IReadOnlyList<Guid> dayIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes a registered character's experience points by <paramref name="amount"/> and records the change against
    /// the Open event. Lowering them must not make the character's free points negative.
    /// </summary>
    Task<LarpEventResult<ParticipantSummary>> ChangeExperienceAsync(
        Guid eventId,
        Guid characterId,
        int amount,
        CancellationToken cancellationToken = default);
}
