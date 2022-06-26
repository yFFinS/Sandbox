namespace Checkers.Core;

public static class EnumerableExtensions
{
    public static bool AllEqual<T>(this IEnumerable<T?> source)
    {
        var haveFirstItem = false;
        var firstItem = default(T);
        foreach (var item in source)
        {
            if (!haveFirstItem)
            {
                firstItem = item;
                haveFirstItem = true;
                continue;
            }

            if (firstItem is null && item is not null || firstItem is not null && item is null)
            {
                return false;
            }

            if (ReferenceEquals(firstItem, item))
            {
                continue;
            }

            if (!firstItem!.Equals(item))
            {
                return false;
            }
        }

        return true;
    }

    public static bool AllSequencesEqual<TItem>(this IEnumerable<IEnumerable<TItem?>?> source)
    {
        var haveFirstSequence = false;
        var firstSequence = default(IEnumerable<TItem?>);

        foreach (var sequence in source)
        {
            if (!haveFirstSequence)
            {
                haveFirstSequence = true;
                firstSequence = sequence?.ToArray();
                continue;
            }

            if (firstSequence is null && sequence is not null || firstSequence is not null && sequence is null)
            {
                return false;
            }

            if (ReferenceEquals(firstSequence, sequence))
            {
                continue;
            }

            if (!firstSequence!.SequenceEqual(sequence!))
            {
                return false;
            }
        }

        return true;
    }
}