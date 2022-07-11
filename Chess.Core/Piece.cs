namespace Chess.Core;

public readonly struct Piece : IEquatable<Piece>
{
    public static readonly Piece Empty = new((PieceColor) byte.MaxValue, (PieceType) byte.MaxValue);

    public Piece(PieceColor color, PieceType type)
    {
        Color = color;
        Type = type;
    }

    public bool IsEmpty => (byte) Type == byte.MaxValue && (byte) Color == byte.MaxValue;

    public readonly PieceColor Color;
    public readonly PieceType Type;

    public bool Equals(Piece other)
    {
        return Type == other.Type && Color == other.Color;
    }

    public override bool Equals(object? obj)
    {
        return obj is Piece other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Color, Type);
    }

    public static bool operator ==(Piece left, Piece right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Piece left, Piece right)
    {
        return !(left == right);
    }
}