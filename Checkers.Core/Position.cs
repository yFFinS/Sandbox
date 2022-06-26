namespace Checkers.Core;

public struct Position : IEquatable<Position>
{
    public int X { get; set; }
    public int Y { get; set; }

    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }

    public readonly Position Offset(int dx, int dy, int distance = 1)
    {
        return new Position(X + dx * distance, Y + dy * distance);
    }

    public readonly Position OffsetTowards(Position position, int distance = 1)
    {
        var (dx, dy) = DirectionTo(position);
        return Offset(dx, dy, distance);
    }

    public readonly bool Equals(Position other)
    {
        return X == other.X && Y == other.Y;
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is Position other && Equals(other);
    }

    public readonly override int GetHashCode()
    {
        unchecked
        {
            return (X * 397) ^ Y;
        }
    }

    public static bool operator ==(Position left, Position right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Position left, Position right)
    {
        return !left.Equals(right);
    }

    public readonly (int dx, int dy) DirectionTo(Position to)
    {
        return (Math.Sign(to.X - X), Math.Sign(to.Y - Y));
    }

    public readonly int DistanceTo(Position to)
    {
        return Math.Abs(to.X - X);
    }
}