namespace Chess.Core.Solver;

public class BoardSearch
{
    private readonly BoardHeuristicAnalyzer _analyzer;
    private readonly SearchConfig _config;

    private TextWriter? _logger;

    public BoardSearch(BoardHeuristicAnalyzer analyzer)
    {
        _analyzer = analyzer;
        _config = new SearchConfig();
    }

    public void EnableLogging(TextWriter logger)
    {
        _logger = logger;
    }

    public void DisableLogging()
    {
        _logger = null;
    }

    public void Configure(Action<SearchConfig> configurator)
    {
        configurator(_config);
    }

    public Move SearchBestMove(Board board)
    {
        var boardCopy = new Board(board.ToFen());

        var alphaBeta = new BoardAlphaBeta(boardCopy, _analyzer, _config.MaxEvaluationTime);
        return alphaBeta.Search().BestMove;
    }
}