namespace Checkers.Core;

public class BoardState
{
    public int StalemateTurns { get; set; } = 0;
    public PieceColor Turn { get; set; } = PieceColor.White;
    public IReadOnlyList<PieceOnBoard> Pieces { get; set; } = ArraySegment<PieceOnBoard>.Empty;
    public int TurnCount { get; set; } = 0;
}