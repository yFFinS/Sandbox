using System.Diagnostics;
using Chess.Core;
using Chess.Core.Solver;
using Chess.View;

//
// var game = new ChessGameMain(args);
// game.Run();


var board = new ChessBoard();
for (var depth = 1; depth <= 5; depth++)
{
    Console.WriteLine($"Depth: {depth}, Nodes: {MoveGenerator.Perft(ChessBoard.StartFEN, depth)}.");
}