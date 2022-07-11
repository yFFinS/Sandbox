namespace Chess.Core.Solver;

public struct EvaluatedMove
{
    public Move Move { get; init; }
    public IReadOnlyList<Move> FullMoveSequence { get; init; }
    public double Score { get; init; }
}