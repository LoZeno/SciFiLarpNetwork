using SciFiLarpNetwork.Backend.Application.CharacterCards;
using SciFiLarpNetwork.Backend.Application.LarpEvents;

namespace SciFiLarpNetwork.Backend.Infrastructure.LarpEvents;

internal sealed class LarpEventCardService(ILarpEventParticipantService participants, ICharacterCardService cards)
    : ILarpEventCardService
{
    public async Task<LarpEventResult<IReadOnlyList<CharacterCard>>> GetCardsAsync(
        Guid eventId,
        Guid? dayId,
        CancellationToken cancellationToken = default)
    {
        var found = await participants.GetParticipantsAsync(eventId, dayId, cancellationToken);
        if (found.Value is not { } registered)
        {
            return Failure(found.Outcome);
        }

        if (registered.Count == 0)
        {
            return Failure(LarpEventOutcome.NoParticipants);
        }

        if (registered.Count > ICharacterCardService.MaxCards)
        {
            return Failure(LarpEventOutcome.TooManyCards);
        }

        var result = await cards.GetCardsAsync(registered.Select(participant => participant.CharacterId).ToArray(), cancellationToken);
        if (result.Outcome != CharacterCardsOutcome.Succeeded)
        {
            return Failure(LarpEventOutcome.UnknownCharacter);
        }

        return LarpEventResult<IReadOnlyList<CharacterCard>>.Success(
            result.Cards
                .Zip(registered, (card, participant) => card with
                {
                    Validity = new CardValidity(participant.Dates[0], participant.Dates[^1])
                })
                .ToArray());
    }

    private static LarpEventResult<IReadOnlyList<CharacterCard>> Failure(LarpEventOutcome outcome) =>
        LarpEventResult<IReadOnlyList<CharacterCard>>.Failure(outcome);
}
