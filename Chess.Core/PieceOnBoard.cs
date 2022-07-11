namespace Chess.Core;

public readonly struct PieceOnBoard
{
    public readonly int Position;
    public readonly Piece Piece;

    public PieceOnBoard(Piece piece, int position)
    {
        Position = position;
        Piece = piece;
    }
}