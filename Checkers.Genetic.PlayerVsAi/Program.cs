using System.Text.Json;
using Checkers.Core;
using Checkers.View;

const string configPath = "ai_config.json";

HeuristicAnalyzerConfig analyzerConfig;
using (var stream = File.OpenRead(configPath))
{
    var tempConfig = JsonSerializer.Deserialize<HeuristicAnalyzerConfig>(stream);

    analyzerConfig = tempConfig ?? throw new JsonException("Cannot load config.");
}

var game = new CheckersGameMain(args);

var player = new PlayerController();
{
    var ai = new AiController();

    ai.Analyzer.Configure(analyzerConfig);
    ai.Solver.Configure(config =>
    {
        config.MaxSearchDepth = 15;
        config.MaxEvaluationTime = 1f;
    });

    game.SetBlackPlayer(ai);
}

game.SetWhitePlayer(player);

game.Run();