namespace SciFiLarpNetwork.Backend.Application.Skills;

/// <summary>
/// Administrator-defined card flags. They are created through skill commands and only matter on character cards.
/// </summary>
public interface ICardFlagService
{
    Task<IReadOnlyList<CardFlagSummary>> GetCardFlagsAsync(CancellationToken cancellationToken = default);
}
