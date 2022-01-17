using System.Collections.Generic;
using System.Linq;

namespace Rebus.ServiceProvider.Internals;

public static class EnumerableExtensions
{
    /// <summary>
    ///     Returns the Cartesian product of the given collection of collections.
    /// </summary>
    public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
    {
        IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
        return sequences.Aggregate(
            emptyProduct,
            (accumulator, sequence) =>
                from acc in accumulator
                from item in sequence
                select acc.Concat(new[] { item }));
    }
}