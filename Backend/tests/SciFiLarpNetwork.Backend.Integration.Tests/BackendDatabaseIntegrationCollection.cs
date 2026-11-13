using System.Diagnostics.CodeAnalysis;

namespace SciFiLarpNetwork.Backend.Tests.Integration;

[CollectionDefinition("Backend database integration", DisableParallelization = true)]
[SuppressMessage(
    "Naming",
    "CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
    Justification = "xUnit's CollectionDefinition type convention requires the Collection suffix.")]
public sealed class BackendDatabaseIntegrationCollection
{
}
