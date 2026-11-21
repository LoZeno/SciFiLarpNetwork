using SciFiLarpNetwork.Backend.Application.CharacterCards;

namespace SciFiLarpNetwork.Backend.Application.LarpEvents;

/// <summary>Collects the character cards of an event's participants.</summary>
public interface ILarpEventCardService
{
    /// <summary>
    /// Builds one card per character registered for <paramref name="dayId"/>, or for any day when it is
    /// <see langword="null"/>, in character running-number order. Each card's validity is that character's first and
    /// last registered day of the event.
    /// </summary>
    Task<LarpEventResult<IReadOnlyList<CharacterCard>>> GetCardsAsync(
        Guid eventId,
        Guid? dayId,
        CancellationToken cancellationToken = default);
}
