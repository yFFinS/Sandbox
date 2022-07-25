namespace Chess.Core;

public readonly struct PieceOnBoard
{
    public readonly int Square;
    public readonly Piece Piece;

    public PieceOnBoard(Piece piece, int square)
    {
        Square = square;
        Piece = piece;
    }
}