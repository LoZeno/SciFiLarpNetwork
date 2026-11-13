using SciFiLarpNetwork.Backend.Application.Skills;

namespace SciFiLarpNetwork.Backend.Api.Skills;

public sealed record CardFlagCollectionProperties(string Title, IReadOnlyList<CardFlagSummary> CardFlags);
