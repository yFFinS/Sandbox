namespace Checkers;

public struct Position : IEquatable<Position>
{
    public int X { get; set; }
    public int Y { get; set; }

    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }

    public Position Offset(int dx, int dy, int distance = 1)
    {
        return new Position(X + dx * distance, Y + dy * distance);
    }

    public Position OffsetTowards(Position position, int distance = 1)
    {
        var (dx, dy) = DirectionTo(position);
        return Offset(dx, dy, distance);
    }

    public bool Equals(Position other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        return obj is Position other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public static bool operator ==(Position left, Position right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Position left, Position right)
    {
        return !left.Equals(right);
    }

    public (int dx, int dy) DirectionTo(Position to)
    {
        return (Math.Sign(to.X - X), Math.Sign(to.Y - Y));
    }

    public int DistanceTo(Position to)
    {
        return Math.Abs(to.X - X);
    }
}