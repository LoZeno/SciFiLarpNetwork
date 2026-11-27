namespace SciFiLarpNetwork.Management.Tests.ViewModels.Items;

internal sealed class FakeItem(Guid id, string name, Guid itemTypeId, Guid rarityId)
{
    public Guid Id { get; } = id;

    public string Name { get; set; } = name;

    public Guid ItemTypeId { get; set; } = itemTypeId;

    public Guid RarityId { get; set; } = rarityId;

    public string? Description { get; set; }

    public string? Effect { get; set; }

    public int Cost { get; set; }

    public int? Charges { get; set; }

    public DateOnly? ExpiresOn { get; set; }

    public Guid? ImageId { get; set; }

    public List<(Guid ItemId, int Quantity)> Components { get; } = [];

    public bool IsArchived { get; set; }

    public long Version { get; set; } = 1;
}

internal sealed class FakeCatalogEntry(Guid id, string name)
{
    public Guid Id { get; } = id;

    public string Name { get; set; } = name;

    public bool IsArchived { get; set; }

    public long Version { get; set; } = 1;
}

internal sealed class FakeCopy(Guid id, Guid itemId, Guid printId)
{
    public Guid Id { get; } = id;

    public Guid ItemId { get; } = itemId;

    public Guid PrintId { get; } = printId;

    public bool IsActive { get; set; }
}
