using System.Diagnostics;

namespace Checkers.Core;

public class BoardSolver
{
    public readonly BoardHeuristicAnalyzer Analyzer;
    public readonly SolverConfig Config;

    private TextWriter? _logger;

    private bool _extractingFullPath;
    private PieceColor _fromPerspective;

    public BoardSolver(BoardHeuristicAnalyzer analyzer)
    {
        Analyzer = analyzer;
        Config = new SolverConfig();
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
        configurator(Config);
    }

    private static IEnumerable<(Board board, Move move)> GenerateNextMoves(Board board)
    {
        foreach (var move in board.MoveGenerator.GenerateAllMoves())
        {
            var newBoard = board.Copy();
            newBoard.MakeMove(move);
            yield return (newBoard, move);
        }
    }

    private BoardMovesTreeNode BuildMoveTree(Board board, int maxDepth)
    {
        var root = new BoardMovesTreeNode
        {
            Board = board
        };

        ReevaluateTree(root, maxDepth);
        return root;
    }

    private void ReevaluateTree(BoardMovesTreeNode root, int maxDepth)
    {
        if (!Config.UsingMultithreading)
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
            var child = new BoardMovesTreeNode
            {
                Board = newBoard,
                LeadingMove = move,
                Parent = node
            };

            node.Children.Add(child);
        }

        node.IsExpanded = true;
    }

    public EvaluatedMove[] EvaluateMoves(Board board, bool extractFullMoveSequence = false)
    {
        _fromPerspective = board.CurrentTurn;
        _extractingFullPath = extractFullMoveSequence;

        if (Config.MaxEvaluationTime < 0)
        {
            return EvaluateMoves(board, Config.MaxSearchDepth);
        }

        var startTicks = Stopwatch.GetTimestamp();
        var maxSearchDepth = Config.MaxSearchDepth == SolverConfig.UnlimitedSearchDepth
            ? int.MaxValue - 1
            : Config.MaxSearchDepth;

        var lastPassedTime = 0.0f;

        BoardMovesTreeNode? root = null;
        foreach (var depth in Enumerable.Range(1, maxSearchDepth))
        {
            if (root is null)
            {
                root = BuildMoveTree(board, depth);
            }
            else
            {
                ResetTreeScores(root);
                ReevaluateTree(root, depth);
            }

            var lastEvaluated = ToEvaluatedMoves(root);

            var ticks = Stopwatch.GetTimestamp();
            var passedTime = (float)(ticks - startTicks) / Stopwatch.Frequency;
            _logger?.WriteLine("Passed time: {0:F2}s. Evaluated depth: {1}.", passedTime, depth);

            if (root.Children.Count == 1 && !_extractingFullPath)
            {
                return lastEvaluated;
            }

            if (2 * passedTime - lastPassedTime >= Config.MaxEvaluationTime || depth == maxSearchDepth)
            {
                return lastEvaluated;
            }

            lastPassedTime = passedTime;
        }

        throw new Exception();
    }

    public static float GetWinPercentFromScore(int score)
    {
        const float alpha = 4;
        return 100f / (1 + (float)Math.Exp(-2 * alpha * score / Math.Sqrt(int.MaxValue)));
    }

    private EvaluatedMove[] EvaluateMoves(Board board, int maxDepth)
    {
        var root = BuildMoveTree(board, maxDepth);
        return ToEvaluatedMoves(root);
    }

    private EvaluatedMove[] ToEvaluatedMoves(BoardMovesTreeNode root)
    {
        BoardMovesTreeNode? lastNode;

        void FindMoveSequencePath(BoardMovesTreeNode node)
        {
            if (lastNode is not null)
            {
                return;
            }

            var hasAny = false;
            foreach (var child in node.Children.Where(child => child.Score == node.Score))
            {
                hasAny = true;
                FindMoveSequencePath(child);
            }

            if (!hasAny)
            {
                lastNode = node;
            }
        }

        var evaluatedMoves = root.Children.Select(child =>
        {
            List<Move>? fullMoveSequence = null;
            if (_extractingFullPath)
            {
                fullMoveSequence = new List<Move>();
                lastNode = null;
                FindMoveSequencePath(root);

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