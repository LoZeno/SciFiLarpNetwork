namespace SciFiLarpNetwork.Management.ViewModels.Skills;

/// <param name="DisplayName">The name, with a localized archived tag when the talent is archived.</param>
public sealed record TalentListItem(
    Guid TalentId,
    string Name,
    bool IsArchived,
    string Href,
    string SkillsHref,
    string DisplayName);
