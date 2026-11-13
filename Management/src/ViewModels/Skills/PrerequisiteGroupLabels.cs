namespace SciFiLarpNetwork.Management.ViewModels.Skills;

/// <param name="HeadingFormat">Composite format with the group number as <c>{0}</c>.</param>
internal sealed record PrerequisiteGroupLabels(string HeadingFormat, string AddMember, string RemoveGroup, string RemoveMember);
