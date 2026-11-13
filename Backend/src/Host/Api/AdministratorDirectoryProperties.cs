using SciFiLarpNetwork.Backend.Application.Administration;

namespace SciFiLarpNetwork.Backend.Api;

public sealed record AdministratorDirectoryProperties(
    string Title,
    string Description,
    IReadOnlyList<AdministratorSummary> Administrators);
