namespace Checkers.Core;

public class BoardState
{
    public int StalemateTurns { get; set; }
    public PieceColor Turn { get; set; }
    public IReadOnlyList<PieceOnBoard> Pieces { get; set; } = null!;
    public int TurnCount { get; set; }
}