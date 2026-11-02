using System.ComponentModel.DataAnnotations;

namespace SciFiLarpNetwork.Backend.Tests.Api.ContractProbes;

public sealed class ProbeRequest
{
    [Required]
    public string? Name { get; init; }
}
