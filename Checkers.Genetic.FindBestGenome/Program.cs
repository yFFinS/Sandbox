using System.Text.Json;
using Checkers.Genetic;

var lastGeneration = GenerationStorage.LoadLastOrCreateNew();
var simulation = new Simulation();
var bestGenome = simulation.FindBestGenomes(lastGeneration, 1);

var analyzerConfig = bestGenome[0].ToConfig();

const string output = "ai_config.json";

using (var stream = File.OpenWrite(output))
{
    using var writer = new Utf8JsonWriter(stream);
    JsonSerializer.Serialize(writer, analyzerConfig);
}

Console.WriteLine("Best genome configuration:\n");
var values = new List<(string, int)>();
foreach (var propertyInfo in analyzerConfig.GetType().GetProperties())
{
    values.Add((propertyInfo.Name, (int)propertyInfo.GetValue(analyzerConfig)!));
}

foreach (var (name, value) in values.OrderByDescending(v => v.Item2))
{
    Console.WriteLine($"{name} = {value}");
}