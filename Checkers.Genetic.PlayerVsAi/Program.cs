using System.Text.Json;
using Checkers.Core;
using Checkers.View;

const string configPath = "ai_config.json";

HeuristicAnalyzerConfig analyzerConfig;
using (var stream = File.OpenRead(configPath))
{
    var tempConfig = JsonSerializer.Deserialize<HeuristicAnalyzerConfig>(stream);

    analyzerConfig = tempConfig ?? throw new FileNotFoundException("Cannot load config");
}

var game = new GameMain(args);

var player = new PlayerController();
var ai = new AiController();

ai.Analyzer.Configure(analyzerConfig);
ai.Solver.Configure(config =>
{
    config.MaxSearchDepth = 20;
    config.MaxEvaluationTime = 3.5f;
    config.UseMultithreading();
});

game.SetWhitePlayer(player);
game.SetBlackPlayer(ai);

game.Run();