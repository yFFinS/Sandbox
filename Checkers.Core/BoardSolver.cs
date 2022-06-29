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

    private BoardMovesTreeNode BuildMoveTree(Board board, int maxDepth, bool useMultithreading = false)
    {
        var root = new BoardMovesTreeNode
        {
            Board = board
        };

        ReevaluateTree(root, maxDepth, useMultithreading);
        return root;
    }

    private void ReevaluateTree(BoardMovesTreeNode root, int maxDepth, bool useMultithreading = false)
    {
        if (!useMultithreading)
        {
            LazyAlphaBeta(root, maxDepth, int.MinValue, int.MaxValue, true);
            return;
        }

        if (!root.IsExpanded)
        {
            ExpandChildren(root);
        }

        Parallel.ForEach(root.Children, child =>
            LazyAlphaBeta(child, maxDepth - 1, int.MinValue, int.MaxValue, false));

        root.Score = root.Children.Max(child => child.Score);
    }

    private static void ResetTreeScores(BoardMovesTreeNode node)
    {
        node.Score = 0;
        foreach (var child in node.Children)
        {
            ResetTreeScores(child);
        }
    }

    private static void ExpandChildren(BoardMovesTreeNode node)
    {
        foreach (var (newBoard, move) in GenerateNextMoves(node.Board!))
        {
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

    public EvaluatedMove[] EvaluateMoves(Board board, bool extractFullMoveSequence = false)
    {
        _fromPerspective = board.CurrentTurn;
        _extractingFullPath = extractFullMoveSequence;

        BoardMovesTreeNode? root = null;

        if (_config.MaxEvaluationTime < 0)
        {
            root = BuildMoveTree(board, _config.MaxSearchDepth, _config.UsingMultithreading);
            var result = ToEvaluatedMoves(root);
            FreeUsedBoards(root);
            return result;
        }

        var startTime = Stopwatch.StartNew();
        var maxSearchDepth = _config.MaxSearchDepth == SolverConfig.UnlimitedSearchDepth
            ? int.MaxValue - 1
            : _config.MaxSearchDepth;

        var lastPassedTime = 0.0f;

        foreach (var depth in Enumerable.Range(1, maxSearchDepth))
        {
            if (root is null)
            {
                root = BuildMoveTree(board, depth, _config.UsingMultithreading);
            }
            else
            {
                ResetTreeScores(root);
                ReevaluateTree(root, depth, _config.UsingMultithreading);
            }

            var lastEvaluated = ToEvaluatedMoves(root);
            var passedTime = startTime.ElapsedMilliseconds / 1000f;

            _logger?.WriteLine("Passed time: {0:F2}s. Evaluated depth: {1}.", passedTime, depth);

            if (root.Children.Count == 1 && !_extractingFullPath)
            {
                FreeUsedBoards(root);
                return lastEvaluated;
            }

            if (2 * passedTime - lastPassedTime >= _config.MaxEvaluationTime || depth == maxSearchDepth)
            {
                FreeUsedBoards(root);
                return lastEvaluated;
            }

            lastPassedTime = passedTime;
        }

        throw new Exception();
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
        var evaluatedMoves = root.Children.Select(child =>
        {
            List<Move>? fullMoveSequence = null;
            if (_extractingFullPath)
            {
                fullMoveSequence = new List<Move>();
                var lastNode = FindClosestBestNode(root);

                while (lastNode?.LeadingMove != null)
                {
                    fullMoveSequence.Add(lastNode.LeadingMove.Value);
                    lastNode = lastNode.Parent;
                }

                fullMoveSequence.Reverse();
            }

            return new EvaluatedMove
            {
                Move = child.LeadingMove!.Value,
                Score = child.Score,
                FullMoveSequence = fullMoveSequence
            };
        });

        return evaluatedMoves.ToArray();
    }

    private int LazyAlphaBeta(BoardMovesTreeNode node, int depth, int alpha, int beta, bool isMaximizing)
    {
        var gameEndState = node.Board!.GetGameEndState();
        if (gameEndState != GameEndState.None)
        {
            return node.Score = Analyzer.GetGameEndScore(gameEndState);
        }

        var shouldEndExpansion = !_extractingFullPath
                                 && node.Parent is not null && node.Parent.IsRoot && node.Parent.Children.Count == 1;

        if (!shouldEndExpansion && depth > 0 && !node.IsExpanded)
        {
            ExpandChildren(node);
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