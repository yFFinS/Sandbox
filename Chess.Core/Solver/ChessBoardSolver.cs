using System.Diagnostics;

namespace Chess.Core.Solver;

public class ChessBoardSolver
{
    public readonly ChessBoardHeuristicAnalyzer Analyzer;
    private readonly ChessSolverConfig _config;

    private TextWriter? _logger;

    private bool _extractingFullPath;
    private PieceColor _fromPerspective;

    public ChessBoardSolver(ChessBoardHeuristicAnalyzer analyzer)
    {
        Analyzer = analyzer;
        _config = new ChessSolverConfig();
    }

    public void EnableLogging(TextWriter logger)
    {
        _logger = logger;
    }

    public void DisableLogging()
    {
        _logger = null;
    }

    public void Configure(Action<ChessSolverConfig> configurator)
    {
        configurator(_config);
    }

    private static IEnumerable<(ChessBoard board, Move move)> GenerateNextMoves(ChessBoard board)
    {
        foreach (var move in board.MoveGenerator.GetAllMoves())
        {
            // var newBoard = board.Copy();
            // newBoard.MakeMove(move);
            yield return (board, move);
        }
    }

    private void EvaluateTree(BoardMovesTreeNode root, int maxDepth)
    {
        LazyAlphaBeta(root, maxDepth, double.MinValue, double.MaxValue, true);
    }

    private int _currentTreeDepth;

    private void ExpandNode(BoardMovesTreeNode node)
    {
        node.Children.Clear();
        foreach (var (newBoard, move) in GenerateNextMoves(node.Board!))
        {
            if (IsTimeExpired)
            {
                return;
            }

            node.Children.Add(
                new BoardMovesTreeNode
                {
                    Board = newBoard,
                    LeadingMove = move,
                    Parent = node
                });
        }

        node.IsExpanded = true;
    }

    private BoardMovesTreeNode? _tree;

    public EvaluatedMove[] EvaluateMoves(ChessBoard board, bool extractFullMoveSequence = false)
    {
        _stopwatch = null;
        _isTimeExpired = false;
        _fromPerspective = board.ColorToMove;
        _extractingFullPath = extractFullMoveSequence;

        _tree = new BoardMovesTreeNode
        {
            Board = board
        };

        if (_config.MaxEvaluationTime < 0)
        {
            var stopwatch = Stopwatch.StartNew();

            EvaluateTree(_tree, _config.HardSearchDepthCap);
            _currentTreeDepth = Math.Max(_currentTreeDepth, _config.HardSearchDepthCap);
            var result = ToEvaluatedMoves(_tree);

            _logger?.WriteLine("Passed time: {0:F2}s. Evaluated depth: {1}.", stopwatch.Elapsed.TotalSeconds,
                _config.HardSearchDepthCap);
            return result;
        }

        var maxSearchDepth = _config.HardSearchDepthCap == ChessSolverConfig.UnlimitedSearchDepth
            ? int.MaxValue - 1
            : _config.HardSearchDepthCap;

        var lastPassedTime = 0.0;

        _stopwatch = Stopwatch.StartNew();

        for (var depth = 1; depth <= maxSearchDepth; depth++)
        {
            EvaluateTree(_tree, depth);

            if (IsTimeExpired)
            {
                return ToEvaluatedMoves(_tree);
            }

            _currentTreeDepth = depth;

            var passedTime = _stopwatch.Elapsed.TotalSeconds;
            _logger?.WriteLine("Passed time: {0:F2}s. Evaluated depth: {1}.", passedTime, depth);

            if (2 * passedTime - lastPassedTime >= _config.MaxEvaluationTime || depth == maxSearchDepth)
            {
                return ToEvaluatedMoves(_tree);
            }

            lastPassedTime = passedTime;
        }

        throw new Exception();
    }

    private Stopwatch? _stopwatch;

    private bool _isTimeExpired;

    private bool IsTimeExpired
    {
        get
        {
            if (_isTimeExpired)
            {
                return true;
            }

            _isTimeExpired = _stopwatch?.Elapsed.TotalSeconds >= _config.MaxEvaluationTime;
            if (_isTimeExpired)
            {
                Console.WriteLine("Time expired");
            }

            return _isTimeExpired;
        }
    }

    public static float GetWinPercentFromScore(int score)
    {
        const float alpha = 0.00001f;
        return 100f / (1 + (float) Math.Exp(-2 * alpha * score));
    }

    private static BoardMovesTreeNode FindClosestBestNode(BoardMovesTreeNode root)
    {
        var queue = new Queue<BoardMovesTreeNode>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (node.Children.Count == 0)
            {
                return node;
            }

            foreach (var child in node.Children.Where(child => child.Score == node.Score))
            {
                queue.Enqueue(child);
            }
        }

        throw new ArgumentException("Invalid tree structure.", nameof(root));
    }

    private EvaluatedMove[] ToEvaluatedMoves(BoardMovesTreeNode root)
    {
        return root.Children
            .Select(child =>
            {
                var fullMoveSequence = new List<Move>();
                var lastNode = FindClosestBestNode(root);

                while (lastNode?.LeadingMove != null)
                {
                    fullMoveSequence.Add(lastNode.LeadingMove.Value);
                    lastNode = lastNode.Parent;
                }

                fullMoveSequence.Reverse();

                return new EvaluatedMove
                {
                    Move = child.LeadingMove!.Value,
                    Score = child.Score,
                    FullMoveSequence = fullMoveSequence
                };
            })
            .ToArray();
    }

    private double LazyAlphaBeta(BoardMovesTreeNode node, int depth, double alpha, double beta, bool isMaximizing)
    {
        if (IsTimeExpired && !node.IsRoot)
        {
            return node.Score;
        }

        var gameEndState = node.Board!.GetGameEndState();
        if (gameEndState != GameEndState.None)
        {
            return node.Score = Analyzer.GetGameEndScore(gameEndState);
        }

        var shouldEndExpansion = !_extractingFullPath
                                 && node.Parent is not null && node.Parent.IsRoot && node.Parent.Children.Count == 1;

        if (!shouldEndExpansion && depth > 0 && !node.IsExpanded)
        {
            ExpandNode(node);
        }

        if (IsTimeExpired)
        {
            return node.Score;
        }

        if (shouldEndExpansion || depth == 0 || node.Children.Count == 0)
        {
            return node.Score = Analyzer.EvaluateBoard(node.Board!, _fromPerspective);
        }

        double value;
        if (isMaximizing)
        {
            value = double.MinValue;
            foreach (var child in node.Children)
            {
                if (IsTimeExpired)
                {
                    return node.Score;
                }

                value = Math.Max(value, LazyAlphaBeta(child, depth - 1, alpha, beta, false));
                if (value >= beta)
                {
                    break;
                }

                alpha = Math.Max(alpha, value);
            }
        }
        else
        {
            value = double.MaxValue;
            foreach (var child in node.Children)
            {
                if (IsTimeExpired)
                {
                    return node.Score;
                }

                value = Math.Min(value, LazyAlphaBeta(child, depth - 1, alpha, beta, true));
                if (value <= alpha)
                {
                    break;
                }

                beta = Math.Min(beta, value);
            }
        }

        return node.Score = value;
    }
}