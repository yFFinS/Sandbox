namespace Checkers;

public readonly struct PieceOnBoard
{
    public readonly Position Position;
    public readonly Piece Piece;

    public PieceOnBoard(Position position, Piece piece)
    {
        Position = position;
        Piece = piece;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Position, Piece);
    }
}