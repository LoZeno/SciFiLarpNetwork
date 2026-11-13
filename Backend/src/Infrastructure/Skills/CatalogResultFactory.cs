using SciFiLarpNetwork.Backend.Application.Skills;

namespace SciFiLarpNetwork.Backend.Infrastructure.Skills;

internal static class CatalogResultFactory
{
    extension<T>(CatalogResult<T>)
        where T : class
    {
        public static CatalogResult<T> Success(T value) => new(CatalogOutcome.Succeeded, value);

        public static CatalogResult<T> Failure(CatalogOutcome outcome) => new(outcome, null);
    }
}
