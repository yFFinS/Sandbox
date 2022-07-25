namespace Chess.Core;

public readonly struct PinsInfo
{
    public Bitboard DiagonalPins { get; init; }
    public Bitboard OrthogonalPins { get; init; }
    public Bitboard DiagonalPinnedMoves { get; init; }
    public Bitboard OrthogonalPinnedMoves { get; init; }
    public Bitboard AllPins => DiagonalPins | OrthogonalPins;
}