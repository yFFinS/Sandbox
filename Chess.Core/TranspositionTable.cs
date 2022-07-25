using System.Diagnostics;

namespace Chess.Core;

public class TranspositionTable
{
    private readonly int _maxEntries;
    private readonly int _bucketSize;

    private readonly TableEntry?[][] _buckets;

    public TranspositionTable(int maxEntries, int bucketSize)
    {
        Debug.Assert(maxEntries >= 0);
        Debug.Assert(bucketSize > 0);
        _maxEntries = maxEntries / bucketSize;
        _bucketSize = bucketSize;

        _buckets = new TableEntry?[maxEntries][];
        for (var i = 0; i < maxEntries; i++)
        {
            _buckets[i] = new TableEntry?[bucketSize];
        }
    }

    public void Insert(TableEntry entry)
    {
        lock (_buckets)
        {
            var index = (int)(entry.Hash % (ulong)_maxEntries);
            var bucket = _buckets[index];

            var inserted = false;
            for (var i = 0; i < bucket.Length; i++)
            {
                if (bucket[i].HasValue)
                {
                    continue;
                }

                bucket[i] = entry;
                inserted = true;
                break;
            }

            if (inserted)
            {
                return;
            }

            for (var i = 0; i < bucket.Length; i++)
            {
                if (bucket[i]!.Value.SearchDepth >= entry.SearchDepth)
                {
                    continue;
                }

                bucket[i] = entry;
                break;
            }
        }
    }

    public TableEntry? ProbeLockless(ulong zobristHash)
    {
        var index = (int)(zobristHash % (ulong)_maxEntries);
        var bucket = _buckets[index];

        foreach (var entry in bucket)
        {
            if (entry?.Hash == zobristHash)
            {
                return entry;
            }
        }

        return null;
    }
}