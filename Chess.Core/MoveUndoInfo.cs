namespace Chess.Core;

public readonly struct MoveUndoInfo
{
    public Move Move { get; init; }
    public Piece CapturedPiece { get; init; }

    public int EnPassantFile { get; init; }
    public int HalfMoves { get; init; }

    public Bitboard Checkers { get; init; }
    public ByColorIndexer<PinsInfo> Pins { get; init; }
    public ByColorIndexer<Bitboard> AttackedIgnoreKing { get; init; }
    public CastlingRights CastlingRights { get; init; }
}