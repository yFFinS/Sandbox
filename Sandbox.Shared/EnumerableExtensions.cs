namespace Sandbox.Shared;

public static class EnumerableExtensions
{
    public static int IndexOf<TItem>(this IReadOnlyList<TItem> source, TItem item, int start = 0)
        where TItem : struct, IEquatable<TItem>
    {
        for (var i = start; i < source.Count; i++)
        {
            if (item.Equals(source[i]))
            {
                return i;
            }
        }

        return -1;
    }

    public static bool AllNonEmpty<TItem>(this IEnumerable<TItem> source, Func<TItem, bool> predicate)
    {
        var nonEmpty = false;

        foreach (var item in source)
        {
            nonEmpty = true;
            if (!predicate(item))
            {
                return false;
            }
        }

        return nonEmpty;
    }
}