using System.Diagnostics;
using Chess.Core;
using Chess.Core.Solver;
using Chess.View;


void Play()
{
    var game = new ChessGameMain(args);
    game.Run();
}

void Divide(int depth, string position)
{
    var divide = MoveGenerator.Divide(position, depth);
    Console.WriteLine($"Divide at depth {depth}. Move count: {divide.Values.Sum(arg => (long)arg)}");
    foreach (var (key, nodes) in divide)
    {
        Console.WriteLine($"{key}: {nodes}");
    }
}

void Perft(int depth, string position)
{
    // Прогрев
    _ = MoveGenerator.Perft(position, 3);

    var stopwatch = Stopwatch.StartNew();
    var data = MoveGenerator.Perft(position, depth);
    var totalSeconds = stopwatch.Elapsed.TotalSeconds;

    Console.WriteLine($"Depth: {depth}.");
    Console.WriteLine($"Nps: {data["Nodes"] / totalSeconds:F0}.\n");

    foreach (var (type, count) in data)
    {
        Console.WriteLine($"{type}: {count}");
    }
}

// for (var depth = 1; depth <= 6; depth++)
// {
//     Perft(depth, "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
//     Console.WriteLine();
// }

Play();

// var board = new Board(Board.StartFen);
//
// var boardSearch = new BoardSearch(new BoardHeuristicAnalyzer());
// boardSearch.Configure(config =>
// {
//     config.MaxEvaluationTime = 1;
//     config.HardSearchDepthCap = 10;
// });
//
// var ai = new ChessAi(board, boardSearch);
// while (!board.IsGameEnded())
// {
//     board.MakeMove(ai.GetNextMove());
// }
//
// Console.WriteLine(board.GetGameEndState());