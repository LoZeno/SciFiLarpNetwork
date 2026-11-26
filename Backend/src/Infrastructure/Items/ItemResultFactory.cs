using SciFiLarpNetwork.Backend.Application.Items;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items;

internal static class ItemResultFactory
{
    extension<T>(ItemResult<T>)
        where T : class
    {
        public static ItemResult<T> Success(T value) => new(ItemOutcome.Succeeded, value, []);

        public static ItemResult<T> Failure(ItemOutcome outcome) => new(outcome, null, []);

        public static ItemResult<T> Unknown(ItemOutcome outcome, IReadOnlyList<Guid> unknownIds) => new(outcome, null, unknownIds);
    }
}
