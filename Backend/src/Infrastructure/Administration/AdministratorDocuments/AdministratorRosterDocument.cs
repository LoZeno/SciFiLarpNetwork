namespace SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;

/// <summary>The single administrator-roster stream's state: which account's role changed last.</summary>
public sealed record AdministratorRosterDocument(Guid Id, Guid LastChangedAccountId)
{
    /// <summary>The well-known id of the one administrator-roster stream.</summary>
    public static readonly Guid StreamId = new("0c5e8f2a-7d41-4b93-a6e0-5f3b2c1d9e84");

    /// <summary>The stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
