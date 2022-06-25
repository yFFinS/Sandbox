namespace Checkers;

public readonly struct Piece
{
    public static readonly Piece WhitePawn = new(PieceType.Pawn, PieceColor.White);
    public static readonly Piece BlackPawn = new(PieceType.Pawn, PieceColor.Black);
    public static readonly Piece WhiteQueen = new(PieceType.Queen, PieceColor.White);
    public static readonly Piece BlackQueen = new(PieceType.Queen, PieceColor.Black);
    public static readonly Piece Empty = new((PieceType)(-1), (PieceColor)(-1));

    public readonly PieceType Type;
    public readonly PieceColor Color;

    public Piece(PieceType type, PieceColor color)
    {
        Type = type;
        Color = color;
    }

    public bool IsEmpty => Type == (PieceType)(-1) || Color == (PieceColor)(-1);
}