namespace Checkers.Genetic;

public static class GenomeFactory
{
    private static readonly Random Random = new(389510931);

    private static float NextPercentDeviation(int maxDeviation)
    {
        var randomValue = Random.NextSingle();
        return 2 * (randomValue - 0.5f) * maxDeviation / 100f;
    }

    private static int NextFlatDeviation(int maxFlatDeviation)
    {
        return Random.Next(-maxFlatDeviation, maxFlatDeviation);
    }

    private static int DeviateValue(int value, float percent, int flat)
    {
        return (int)MathF.Round(value * (1 + percent)) + flat;
    }

    public static void MutateGenome(Genome genome, int maxPercentDeviation, int maxFlatDeviation)
    {
        for (var i = 0; i < genome.Length; i++)
        {
            var percentDeviation = NextPercentDeviation(maxPercentDeviation);
            var flatDeviation = NextFlatDeviation(maxFlatDeviation);
            genome[i] = DeviateValue(genome[i], percentDeviation, flatDeviation);
        }
    }

    public static Genome CombineGenomes(Genome left, Genome right)
    {
        if (left.Length != right.Length)
        {
            throw new InvalidOperationException();
        }

        var length = left.Length;
        var values = new int[length];

        for (var i = 0; i < length; i++)
        {
            var alpha = Random.NextSingle();
            values[i] = (int)MathF.Round(left[i] * alpha + right[i] * (1 - alpha));
        }

        return new Genome(values);
    }
}