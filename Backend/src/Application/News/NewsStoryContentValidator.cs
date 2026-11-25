using SciFiLarpNetwork.Backend.Domain.News;

namespace SciFiLarpNetwork.Backend.Application.News;

public static class NewsStoryContentValidator
{
    /// <summary>Checks the rules that need no stored data.</summary>
    /// <returns><see langword="null"/> when the content is valid; otherwise the outcome of the first failed check.</returns>
    public static NewsOutcome? Validate(NewsStoryContent content) =>
        !NewsTexts.IsValidTitle(content.Title) ? NewsOutcome.InvalidTitle
        : !NewsTexts.IsValidBody(content.Body) ? NewsOutcome.InvalidBody
        : !NewsTexts.AreValidTimes(content.StartsAt, content.EndsAt) ? NewsOutcome.InvalidTimes
        : !NewsTexts.IsValidHackingDifficulty(content.HackingDifficulty) ? NewsOutcome.InvalidHackingDifficulty
        : null;
}
