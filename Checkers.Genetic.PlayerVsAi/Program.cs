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
var ai = new AiController();

ai.Analyzer.Configure(analyzerConfig);
ai.Solver.Configure(config =>
{
    config.MaxSearchDepth = 15;
    config.MaxEvaluationTime = 1f;
});

game.Board.SetState(new BoardState
{
    Pieces = new[]
    {
        new PieceOnBoard(new Position(3, 2), Piece.BlackPawn),
        new PieceOnBoard(new Position(1, 2), Piece.BlackPawn),
        new PieceOnBoard(new Position(1, 4), Piece.BlackPawn),
        new PieceOnBoard(new Position(3, 4), Piece.BlackPawn),
        new PieceOnBoard(new Position(5, 4), Piece.BlackPawn),
        new PieceOnBoard(new Position(5, 2), Piece.BlackPawn),
        new PieceOnBoard(new Position(6, 5), Piece.WhitePawn),
    }
});
game.SetWhitePlayer(player);
game.SetBlackPlayer(ai);

game.Run();