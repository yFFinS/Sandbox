using System.Text.Json;

namespace Checkers.Genetic;

public static class GenerationStorage
{
    private static readonly string DirectoryPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Generations");

    private const string GenerationNamePattern = "gen_{0}.json";

    private static string GetPathFromId(int id)
    {
        return Path.Combine(DirectoryPath, string.Format(GenerationNamePattern, id));
    }

    public static Generation LoadGeneration(int id)
    {
        EnsureDirectoryExists();
        var path = GetPathFromId(id);
        return LoadGeneration(path);
    }

    private static Generation LoadGeneration(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var reader = new Utf8JsonReader(bytes);
        return Generation.FromJson(ref reader);
    }

    public static Generation LoadLastOrCreateNew()
    {
        EnsureDirectoryExists();

        var files = Directory.EnumerateFiles(DirectoryPath);
        var lastGenerationFileName = files.MaxBy(file =>
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var generationIdString = fileName.Split('_')[^1];
            return Convert.ToInt32(generationIdString);
        });

        if (!string.IsNullOrEmpty(lastGenerationFileName))
        {
            return LoadGeneration(lastGenerationFileName);
        }

        var generation = Generation.CreateNew(GenerationRules.Default);
        SaveGeneration(generation);
        return generation;
    }

    public static void SaveGeneration(Generation generation, bool canOverride = false)
    {
        EnsureDirectoryExists();

        var path = GetPathFromId(generation.Id);
        if (File.Exists(path) && !canOverride)
        {
            throw new InvalidOperationException($"Generation with id {generation.Id} already exists.");
        }

        using var stream = File.OpenWrite(path);
        var writer = new Utf8JsonWriter(stream);
        generation.ToJson(writer);
    }

    private static void EnsureDirectoryExists()
    {
        if (!Directory.Exists(DirectoryPath))
        {
            Directory.CreateDirectory(DirectoryPath);
        }
    }
}