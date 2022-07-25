namespace Chess.Core;

public readonly struct Move : IEquatable<Move>
{
    public int Start { get; init; }

    public int End { get; init; }

    public MoveType Type { get; init; }

    public override string ToString()
    {
        var str = Board.GetSquareName(Start) + Board.GetSquareName(End);
        switch (Type)
        {
            case MoveType.BishopPromotionQuiet or MoveType.BishopPromotionCapture:
                str += 'b';
                break;
            case MoveType.KnightPromotionQuiet or MoveType.KnightPromotionCapture:
                str += 'n';
                break;
            case MoveType.RookPromotionQuiet or MoveType.RookPromotionCapture:
                str += 'r';
                break;
            case MoveType.QueenPromotionQuiet or MoveType.QueenPromotionCapture:
                str += 'q';
                break;
        }

        return str;
    }

    public IEnumerable<int> GetPath()
    {
        yield return Start;
        yield return End;
    }

    public bool IsEmpty => Start == 0 && End == 0;

    public static readonly Move Empty = new()
    {
        Start = 0,
        End = 0
    };

    public bool Equals(Move other)
    {
        return Start == other.Start && End == other.End && Type == other.Type;
    }

    public override bool Equals(object? obj)
    {
        return obj is Move other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Start, End, (int)Type);
    }

    public static bool operator ==(Move left, Move right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Move left, Move right)
    {
        return !left.Equals(right);
    }
}