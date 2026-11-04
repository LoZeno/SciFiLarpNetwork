using System.ComponentModel.DataAnnotations;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Api.ContractProbes;

public sealed class ProbeRequest
{
    [Required]
    public string? Name { get; init; }
}
