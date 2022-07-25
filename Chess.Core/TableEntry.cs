namespace Chess.Core;

public readonly struct TableEntry
{
    public ulong Hash { get; init; }
    public Move BestMove { get; init; }
    public int Value { get; init; }
    public EntryValueType Type { get; init; }
    public int SearchDepth { get; init; }

    public int? Apply(int depth, int alpha, int beta)
    {
        if (SearchDepth < depth)
        {
            return null;
        }

        switch (Type)
        {
            case EntryValueType.Alpha:
                if (Value <= alpha)
                {
                    return alpha;
                }

                break;
            case EntryValueType.Beta:
                if (Value >= beta)
                {
                    return beta;
                }

                break;
            case EntryValueType.Exact:
                return Value;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return null;
    }
}