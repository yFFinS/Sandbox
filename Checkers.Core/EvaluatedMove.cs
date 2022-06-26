namespace Checkers.Core;

public class EvaluatedMove
{
    public Move Move { get; set; }
    public IReadOnlyList<Move>? FullMoveSequence { get; set; }
    public int Score { get; set; }
}