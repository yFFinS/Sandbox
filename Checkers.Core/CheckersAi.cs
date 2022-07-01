namespace Checkers.Core;

public class CheckersAi
{
    private readonly Board _board;
    private TextWriter? _logger;
    public readonly BoardSolver Solver;

    public CheckersAi(Board board, BoardSolver solver)
    {
        _board = board;
        Solver = solver;
    }

    public void EnableLogging(TextWriter logger)
    {
        _logger = logger;
    }

    public void DisableLogging()
    {
        _logger = null;
    }

    public void SelectMove(Move move)
    {
        Solver.SelectBranch(move);
    }

    public EvaluatedMove GetNextMove(bool extractFullMoveSequence = false)
    {
        var moves = RateMoves(extractFullMoveSequence);
        var bestMove = moves[0];
        foreach (var evaluatedMove in moves)
        {
            if (bestMove.Score >= evaluatedMove.Score)
            {
                continue;
            }

            bestMove = evaluatedMove;
        }

        _logger?.WriteLine("Selected move with score: {0}.", bestMove.Score);
        _logger?.WriteLine("Estimated win percent: {0:F2}%", BoardSolver.GetWinPercentFromScore(bestMove.Score));

        return bestMove;
    }

    private EvaluatedMove[] RateMoves(bool extractFullMoveSequence = false)
    {
        return Solver.EvaluateMoves(_board, extractFullMoveSequence);
    }

    public async Task<EvaluatedMove> GetNextMoveAsync(bool extractFullMoveSequence = false)
    {
        return await Task.Run(() => GetNextMove(extractFullMoveSequence));
    }

    public void OnGameStarted()
    {
        Solver.ResetTree();
    }
}