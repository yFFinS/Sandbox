using System.Runtime.CompilerServices;

namespace Chess.Core;

public readonly struct Piece : IEquatable<Piece>
{
    public static readonly Piece Empty = new((PieceColor)byte.MaxValue, (PieceType)byte.MaxValue);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Piece(PieceColor color, PieceType type)
    {
        Color = color;
        Type = type;
    }

    public bool IsEmpty => (byte)Type == byte.MaxValue && (byte)Color == byte.MaxValue;

    public readonly PieceColor Color;
    public readonly PieceType Type;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Piece other)
    {
        return Type == other.Type && Color == other.Color;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        return obj is Piece other && Equals(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return HashCode.Combine(Color, Type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Piece left, Piece right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Piece left, Piece right)
    {
        return !(left == right);
    }
}