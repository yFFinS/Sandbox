using Chess.Core.Solver;

namespace Chess.Core;

public class ChessAi
{
    private readonly ChessBoard _board;
    private readonly ChessBoardSolver _solver;

    public ChessAi(ChessBoard board, ChessBoardSolver solver)
    {
        _board = board;
        _solver = solver;
    }

    public Move GetNextMove()
    {
        var moves = _solver.EvaluateMoves(_board);
        return moves.Length == 0 ? new Move() : moves.MaxBy(move => move.Score).Move;
    }
}