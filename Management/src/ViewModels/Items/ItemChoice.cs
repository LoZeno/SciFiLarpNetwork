namespace SciFiLarpNetwork.Management.ViewModels.Items;

/// <summary>An item type, rarity or item offered in a list; <see cref="Id"/> is <see langword="null"/> for “all types”.</summary>
public sealed record ItemChoice(Guid? Id, string Name)
{
    public override string ToString() => Name;
}
