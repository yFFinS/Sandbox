namespace Checkers.Core;

public class SolverConfig
{
    public const int UnlimitedSearchDepth = -1;
    public const float UnlimitedTime = -1;

    public int MaxSearchDepth { get; set; } = 3;
    public bool UsingMultithreading { get; private set; } = false;
    public float MaxEvaluationTime { get; set; } = UnlimitedTime;

    public void DoNotLimitTime()
    {
        MaxEvaluationTime = UnlimitedTime;
    }

    public void DoNotLimitSearchDepth()
    {
        MaxSearchDepth = UnlimitedSearchDepth;
    }

    public void UseMultithreading()
    {
        UsingMultithreading = true;
    }
}