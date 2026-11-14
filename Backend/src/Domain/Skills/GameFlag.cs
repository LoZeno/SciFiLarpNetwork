using System.Diagnostics.CodeAnalysis;

namespace SciFiLarpNetwork.Backend.Domain.Skills;

/// <summary>
/// Product-defined skill flags used by player-facing features. An improvement flag only counts when the
/// character also owns a skill with the matching access flag.
/// </summary>
/// <remarks>
/// Members are persisted by name, so renaming a member is a breaking change to stored events.
/// </remarks>
[SuppressMessage(
    "Naming",
    "CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
    Justification = "\"Game flag\" is the domain term used by the MANAGER-02 specification.")]
public enum GameFlag
{
    AccessHacking,
    ImprovesHacking,
    AccessCybersecurity,
    ImprovesCybersecurity,
    AccessMedicalChemistryLab,
    ImprovesMedicalChemistry,
    AccessBiochemistryLab,
    ImprovesBiochemistry,
    AccessTechnicalWorkbench,
    ImprovesItemCreation,
    GivesChemicalIdentification,
    GivesToxinIdentification
}
