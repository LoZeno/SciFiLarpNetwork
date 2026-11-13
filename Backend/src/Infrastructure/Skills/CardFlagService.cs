using Marten;
using SciFiLarpNetwork.Backend.Application.Skills;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;
using SciFiLarpNetwork.Backend.Infrastructure.Skills.SkillDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Skills;

internal sealed class CardFlagService(AttributedSessionFactory sessions) : ICardFlagService
{
    public async Task<IReadOnlyList<CardFlagSummary>> GetCardFlagsAsync(CancellationToken cancellationToken = default)
    {
        await using var query = sessions.QuerySession();
        var cardFlags = await query.Query<CardFlagDocument>().ToListAsync(cancellationToken);
        return cardFlags
            .OrderBy(cardFlag => cardFlag.NormalizedName, StringComparer.Ordinal)
            .Select(cardFlag => cardFlag.ToSummary())
            .ToArray();
    }
}
