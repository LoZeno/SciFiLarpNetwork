namespace SciFiLarpNetwork.Management.ViewModels.Skills;

/// <param name="PrerequisiteCandidates">Every skill the edited skill may require, archived ones included.</param>
internal sealed record SkillEditorChoices(
    IReadOnlyList<SelectableOption> Talents,
    IReadOnlyList<SelectableOption> GameFlags,
    IReadOnlyList<SelectableOption> CardFlags,
    IReadOnlyList<SkillListItem> PrerequisiteCandidates);
