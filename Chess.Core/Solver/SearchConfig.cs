namespace Chess.Core.Solver;

public class SearchConfig
{
    public int HardSearchDepthCap { get; set; } = 3;
    public int SoftSearchDepthCap { get; set; } = 2;
    public bool UsingMultithreading { get; set; } = false;
    public double MaxEvaluationTime { get; set; } = 5000;

    public SearchConfig Copy()
    {
        return (SearchConfig)MemberwiseClone();
    }
}