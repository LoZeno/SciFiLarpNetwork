namespace SciFiLarpNetwork.Backend.Api.Skills;

/// <param name="GameFlags">Every product-defined game flag with its localized title, for skill dialogs.</param>
public sealed record SkillCollectionProperties(
    string Title,
    string Description,
    bool IncludesArchived,
    IReadOnlyList<GameFlagOption> GameFlags);
