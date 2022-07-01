using Checkers.Genetic;
using Checkers.View;

var lastGeneration = GenerationStorage.LoadLastOrCreateNew();
var simulation = new Simulation();
var bestGenomes = simulation.FindBestGenomes(lastGeneration, 2);

Console.WriteLine("Press any key to start playing...");
Console.Read();

var game = new CheckersGameMain(args);

var whiteAi = new AiController();
var blackAi = new AiController();

whiteAi.Analyzer.Configure(bestGenomes[0].ToConfig());
whiteAi.Solver.Configure(config =>
{
    config.MaxSearchDepth = 20;
    config.MaxEvaluationTime = 3.5f;
    config.UseMultithreading();
});

blackAi.Analyzer.Configure(bestGenomes[1].ToConfig());
blackAi.Solver.Configure(config =>
{
    config.MaxSearchDepth = 20;
    config.MaxEvaluationTime = 3.5f;
    config.UseMultithreading();
});

game.SetWhitePlayer(whiteAi);
game.SetBlackPlayer(blackAi);

game.Run();