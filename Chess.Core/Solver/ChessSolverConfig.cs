namespace Chess.Core.Solver;

public class ChessSolverConfig
{
    public const int UnlimitedSearchDepth = -1;
    public const float UnlimitedTime = -1;

    public int HardSearchDepthCap { get; set; } = 3;
    public int SoftSearchDepthCap { get; set; } = 2;
    public bool UsingMultithreading { get; set; } = false;
    public float MaxEvaluationTime { get; set; } = UnlimitedTime;

    public ChessSolverConfig Copy()
    {
        return (ChessSolverConfig) MemberwiseClone();
    }
}