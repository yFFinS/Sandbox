using System.Text.Json.Serialization;
using Checkers.Core;

namespace Checkers.Genetic;

public class Genome
{
    private readonly int[] _value;

    [JsonIgnore] public int Length => _value.Length;

    public int this[int index]
    {
        get => _value[index];
        set => _value[index] = value;
    }

    public IReadOnlyList<int> Value => _value;

    [JsonConstructor]
    public Genome(IReadOnlyList<int> value)
    {
        _value = value.ToArray();
    }
    
    public Genome(IEnumerable<int> value)
    {
        _value = value.ToArray();
    }

    public Genome(Genome source)
    {
        _value = source._value.ToArray();
    }

    public static Genome FromConfig(HeuristicAnalyzerConfig config)
    {
        var properties = config.GetType().GetProperties();

        var value = properties
            .OrderBy(p => p.Name)
            .Select(p => (int)p.GetValue(config)!);

        return new Genome(value);
    }

    public HeuristicAnalyzerConfig ToConfig()
    {
        var config = new HeuristicAnalyzerConfig();
        var properties = config.GetType().GetProperties();

        foreach (var (propertyInfo, genomeValue) in properties.OrderBy(p => p.Name).Zip(_value))
        {
            propertyInfo.SetValue(config, genomeValue);
        }

        return config;
    }
}