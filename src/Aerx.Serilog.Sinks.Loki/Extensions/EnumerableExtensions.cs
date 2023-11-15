namespace Aerx.Serilog.Sinks.Loki.Extensions;

internal static class EnumerableExtensions
{
    internal static (IEnumerable<TSource> Matched, IEnumerable<TSource> Unmatched) Partition<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, bool> predicate)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        var matched = new List<TSource>();
        var unmatched = new List<TSource>();

        foreach (var item in source)
        {
            (predicate(item) ? matched : unmatched).Add(item);
        }

        return (matched, unmatched);
    }
}