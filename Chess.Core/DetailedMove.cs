namespace Chess.Core;

public readonly struct DetailedMove
{
    public Move Move { get; init; }
    public Piece CapturedPiece { get; init; }
    public Piece MovedPiece { get; init; }
}