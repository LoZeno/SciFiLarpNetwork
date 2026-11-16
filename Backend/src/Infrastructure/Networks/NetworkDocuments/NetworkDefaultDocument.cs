namespace SciFiLarpNetwork.Backend.Infrastructure.Networks.NetworkDocuments;

/// <summary>The single network-default stream's state: which network was chosen last.</summary>
public sealed record NetworkDefaultDocument(Guid Id, Guid NetworkId)
{
    /// <summary>The well-known id of the one network-default stream.</summary>
    public static readonly Guid StreamId = new("6f1c2d9e-3b7a-4c55-9d21-8e0a4b7f5c13");

    /// <summary>The stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
