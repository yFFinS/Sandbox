using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Checkers.Core;

namespace Checkers.Genetic;

public class Generation
{
    [JsonInclude] public readonly int Id;
    [JsonInclude] public readonly IReadOnlyList<Genome> Genomes;
    [JsonInclude] public readonly GenerationRules GenerationRules;

    [JsonConstructor]
    public Generation(int id, IReadOnlyList<Genome> genomes, GenerationRules generationRules)
    {
        Id = id;
        Genomes = genomes;
        GenerationRules = generationRules;
    }

    public Generation(int id, IEnumerable<Genome> genomes, GenerationRules generationRules)
    {
        Id = id;
        Genomes = genomes.ToArray();
        GenerationRules = generationRules;
    }

    public static Generation CreateNew(GenerationRules rules)
    {
        var genomeCount = rules.Instances;
        if (genomeCount % 4 != 0)
        {
            throw new ArgumentException("Genome count must be divisible by 4.", nameof(rules));
        }

        var maxPercentDeviation = 15;
        var maxFlatDeviation = 500;

        var genomes = new List<Genome>();
        var defaultAnalyzerConfig = new HeuristicAnalyzerConfig();

        foreach (var _ in Enumerable.Range(0, genomeCount))
        {
            var genome = Genome.FromConfig(defaultAnalyzerConfig);
            GenomeFactory.MutateGenome(genome, maxPercentDeviation, maxFlatDeviation);
            genomes.Add(genome);
        }

        return new Generation(1, genomes, rules);
    }

    public void ToJson(Utf8JsonWriter destination)
    {
        JsonSerializer.Serialize(destination, this);
    }

    public static Generation FromJson(ref Utf8JsonReader reader)
    {
        var generation = JsonSerializer.Deserialize<Generation>(ref reader);
        if (generation is null)
        {
            throw new Exception("Cannot deserialize generation.");
        }

        return generation;
    }
}