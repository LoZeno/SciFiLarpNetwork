using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Domain.Items;

namespace SciFiLarpNetwork.Backend.Api.Items;

/// <param name="Components">The new item's components; none when omitted.</param>
/// <param name="ClonedFromItemId">The item being cloned; the new item also gets its current picture.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ItemCreateRequest(
    [Required] string Name,
    [property: JsonRequired] Guid ItemTypeId,
    [property: JsonRequired] Guid RarityId,
    string? Description,
    string? Effect,
    [property: JsonRequired] int Cost,
    int? Charges,
    DateOnly? ExpiresOn,
    IReadOnlyList<ItemComponentRequest>? Components,
    Guid? ClonedFromItemId)
{
    public ItemCreation ToCreation() =>
        new(
            new ItemDefinition(Name, ItemTypeId, RarityId, Description, Effect, Cost, Charges, ExpiresOn),
            [.. (Components ?? []).Select(component => new ItemComponent(component.ItemId, component.Quantity))],
            ClonedFromItemId);
}
