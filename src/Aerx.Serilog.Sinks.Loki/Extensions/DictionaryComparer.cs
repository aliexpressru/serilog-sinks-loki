namespace Aerx.Serilog.Sinks.Loki.Extensions;

internal class DictionaryComparer<TKey, TValue> : IEqualityComparer<IDictionary<TKey, TValue>>
{
    internal static DictionaryComparer<TKey, TValue> Instance { get; } = new();

    public bool Equals(IDictionary<TKey, TValue> x, IDictionary<TKey, TValue> y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (ReferenceEquals(x, null))
        {
            return false;
        }

        if (ReferenceEquals(y, null))
        {
            return false;
        }

        if (x.GetType() != y.GetType())
        {
            return false;
        }

        return x.Count == y.Count && !x.Except(y).Any();
    }

    public int GetHashCode(IDictionary<TKey, TValue> obj)
    {
        unchecked
        {
            var hash = 17;
            foreach (var kvp in obj.OrderBy(kvp => kvp.Key))
            {
                hash = (hash * 27) + kvp.Key!.GetHashCode();
                hash = (hash * 27) + kvp.Value!.GetHashCode();
            }

            return hash;
        }
    }
}