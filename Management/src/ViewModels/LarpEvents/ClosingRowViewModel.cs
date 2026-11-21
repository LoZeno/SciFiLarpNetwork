namespace SciFiLarpNetwork.Management.ViewModels.LarpEvents;

/// <summary>One participant's row of the closing grid.</summary>
/// <param name="BaseCell">The base-experience cell; <see langword="null"/> when the event has no base experience.</param>
/// <param name="DayCells">One cell per event day, in date order.</param>
public sealed record ClosingRowViewModel(
    Guid CharacterId,
    string CharacterName,
    ClosingCellViewModel? BaseCell,
    IReadOnlyList<ClosingCellViewModel> DayCells)
{
    public bool HasBaseCell => BaseCell is not null;
}
