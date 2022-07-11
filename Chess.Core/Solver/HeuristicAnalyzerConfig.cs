namespace Chess.Core.Solver;

public class HeuristicAnalyzerConfig
{
    public const double VictoryScore = 100;
    public const double DefeatScore = -VictoryScore;
    public const double DrawScore = 0;

    public HeuristicAnalyzerConfig Copy()
    {
        return (HeuristicAnalyzerConfig) MemberwiseClone();
    }

    public double PawnAlive { get; init; } = 1;
    public double RookAlive { get; init; } = 5.3;
    public double BishopAlive { get; init; } = 2.7;
    public double KnightAlive { get; init; } = 3;
    public double QueenAlive { get; init; } = 9;
}