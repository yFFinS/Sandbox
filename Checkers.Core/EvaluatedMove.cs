namespace Checkers.Core;

public struct EvaluatedMove
{
    public Move Move { get; init; }
    public IReadOnlyList<Move>? FullMoveSequence { get; init; }
    public int Score { get; init; }
}