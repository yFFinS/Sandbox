namespace Checkers.Core;

public readonly struct Piece
{
    public static readonly Piece WhitePawn = new(PieceType.Pawn, PieceColor.White);
    public static readonly Piece BlackPawn = new(PieceType.Pawn, PieceColor.Black);
    public static readonly Piece WhiteQueen = new(PieceType.Queen, PieceColor.White);
    public static readonly Piece BlackQueen = new(PieceType.Queen, PieceColor.Black);

    public static readonly Piece Empty = new(byte.MaxValue);

    private readonly byte _value;

    public PieceType Type => (_value & 2) == 0 ? PieceType.Pawn : PieceType.Queen;
    public PieceColor Color => (_value & 1) == 0 ? PieceColor.Black : PieceColor.White;

    public Piece(PieceType type, PieceColor color)
    {
        _value = (byte)((type == PieceType.Pawn ? 0 : 2) + (color == PieceColor.Black ? 0 : 1));
    }

    private Piece(byte value)
    {
        _value = value;
    }

    public bool IsEmpty => _value == byte.MaxValue;

    public byte GetRawValue() => _value;
}