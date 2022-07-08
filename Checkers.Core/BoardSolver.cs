using System.Diagnostics;

namespace Checkers.Core;

public class BoardSolver
{
    public readonly BoardHeuristicAnalyzer Analyzer;
    private readonly SolverConfig _config;

    private TextWriter? _logger;

    private bool _extractingFullPath;
    private PieceColor _fromPerspective;

    public BoardSolver(BoardHeuristicAnalyzer analyzer)
    {
        Analyzer = analyzer;
        _config = new SolverConfig();
    }

    public void EnableLogging(TextWriter logger)
    {
        _logger = logger;
    }

    public void DisableLogging()
    {
        _logger = null;
    }

    public void Configure(Action<SolverConfig> configurator)
    {
        configurator(_config);
    }

    private static IEnumerable<(Board board, Move move)> GenerateNextMoves(Board board)
    {
        foreach (var move in board.MoveGenerator.GenerateAllMoves())
        {
            var newBoard = BoardSolverMemoryAllocator.RequestBoardCopy(board);
            newBoard.MakeMove(move);
            yield return (newBoard, move);
        }
    }

    private void EvaluateTree(BoardMovesTreeNode root, int maxDepth)
    {
        LazyAlphaBeta(root, maxDepth, int.MinValue, int.MaxValue, true);
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

    private static void FreeUsedBoards(BoardMovesTreeNode node)
    {
        if (node.Board is not null && !node.IsRoot)
        {
            BoardSolverMemoryAllocator.FreeBoard(node.Board);
        }

        foreach (var child in node.Children)
        {
            FreeUsedBoards(child);
        }
    }

    private BoardMovesTreeNode? _tree;

    public void SelectBranch(Move move)
    {
        _tree = _tree?.Children.FirstOrDefault(child =>
        {
            var childMove = child.LeadingMove!.Value;
            return childMove.PieceOnBoard.Position == move.PieceOnBoard.Position &&
                   childMove.Path.SequenceEqual(move.Path);
        });

        if (_tree is not null)
        {
            _currentTreeDepth--;
        }
        else
        {
            _currentTreeDepth = 0;
        }
    }

    public void ResetTree()
    {
        _currentTreeDepth = 0;
        _tree = null;
    }

    public EvaluatedMove[] EvaluateMoves(Board board, bool extractFullMoveSequence = false)
    {
        _stopwatch = null;
        _isTimeExpired = false;
        _fromPerspective = board.CurrentTurn;
        _extractingFullPath = extractFullMoveSequence;

        _tree ??= new BoardMovesTreeNode
        {
            Board = board
        };

        if (_config.MaxEvaluationTime < 0)
        {
            var stopwatch = Stopwatch.StartNew();
            
            EvaluateTree(_tree, _config.MaxSearchDepth);
            _currentTreeDepth = Math.Max(_currentTreeDepth, _config.MaxSearchDepth);
            var result = ToEvaluatedMoves(_tree);

            _logger?.WriteLine("Passed time: {0:F2}s. Evaluated depth: {1}.", stopwatch.Elapsed.TotalSeconds, _config.MaxSearchDepth);
            return result;
        }

        var maxSearchDepth = _config.MaxSearchDepth == SolverConfig.UnlimitedSearchDepth
            ? int.MaxValue - 1
            : _config.MaxSearchDepth;

        var lastPassedTime = 0.0;

        _stopwatch = Stopwatch.StartNew();

        for (var depth = _currentTreeDepth + 1; depth <= maxSearchDepth; depth++)
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

            return _isTimeExpired = _stopwatch?.Elapsed.TotalSeconds >= _config.MaxEvaluationTime;
        }
    }

    public static float GetWinPercentFromScore(int score)
    {
        const float alpha = 0.00001f;
        return 100f / (1 + (float)Math.Exp(-2 * alpha * score));
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
                List<Move>? fullMoveSequence = null;
                var evaluatedMove = new EvaluatedMove
                {
                    Move = child.LeadingMove!.Value,
                    Score = child.Score,
                    FullMoveSequence = fullMoveSequence
                };

                if (!_extractingFullPath)
                {
                    return evaluatedMove;
                }

                fullMoveSequence = new List<Move>();
                var lastNode = FindClosestBestNode(root);

                while (lastNode?.LeadingMove != null)
                {
                    fullMoveSequence.Add(lastNode.LeadingMove.Value);
                    lastNode = lastNode.Parent;
                }

                fullMoveSequence.Reverse();

                return evaluatedMove;
            })
            .ToArray();
    }

    private int LazyAlphaBeta(BoardMovesTreeNode node, int depth, int alpha, int beta, bool isMaximizing)
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

        int value;
        if (isMaximizing)
        {
            value = int.MinValue;
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
            value = int.MaxValue;
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