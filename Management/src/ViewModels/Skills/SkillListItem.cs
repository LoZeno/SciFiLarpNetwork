namespace SciFiLarpNetwork.Management.ViewModels.Skills;

/// <param name="DisplayName">The name, with a localized archived tag when the skill is archived.</param>
public sealed record SkillListItem(
    Guid SkillId,
    string Name,
    bool IsArchived,
    string Href,
    string DisplayName);
