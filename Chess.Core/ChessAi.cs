using Chess.Core.Solver;

namespace Chess.Core;

public class ChessAi
{
    private readonly Board _board;
    private readonly BoardSearch _search;

    public ChessAi(Board board, BoardSearch search)
    {
        _board = board;
        _search = search;
    }

    public Move GetNextMove()
    {
        return _search.SearchBestMove(_board);
    }
}