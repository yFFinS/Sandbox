namespace Checkers.Genetic;

public readonly struct GenerationRules
{
    public int MaxSearchDepth { get; init; }
    public float MaxSearchTime { get; init; }
    public int Instances { get; init; }
    
    public static GenerationRules Default { get; } = new()
    {
        MaxSearchDepth = 3,
        Instances = 256,
        MaxSearchTime = 0.25f
    };
}