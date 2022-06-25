using System.Reflection.Metadata.Ecma335;

namespace Checkers;

public class AiConfig
{
    public int MaxSearchDepth { get; set; } = 3;
    public bool UseMultithreading { get; set; }
}

public class CheckersAi
{
    private readonly Board _board;
    private readonly AiConfig _config;

    public CheckersAi(Board board)
    {
        _board = board;
        _config = new AiConfig();
    }

    public void Configure(Action<AiConfig> configurator)
    {
        configurator(_config);
    }

    public Move GetNextMove()
    {
        return BoardSolver.GetBestMove(_board, _config.MaxSearchDepth, _config.UseMultithreading);
    }
}

public class BoardMovesTreeNode
{
    public BoardMovesTreeNode? Parent { get; init; }
    public readonly List<BoardMovesTreeNode> Children = new();

    public Board Board { get; init; } = null!;
    public Move? LeadingMove { get; init; }
    public int CummulativeScore { get; init; }
    public PieceColor Turn => LeadingMove!.PieceOnBoard.Piece.Color;
}

internal static class BoardSolver
{
    private static IEnumerable<(Board board, Move move)> GenerateNextMoves(Board board)
    {
        foreach (var move in board.MoveGenerator.GenerateAllMoves())
        {
            var newBoard = board.Clone();
            newBoard.MakeMove(move);
            yield return (newBoard, move);
        }
    }

    private const int PawnCapturedScore = 250;
    private const int QueenCapturedScore = 1000;
    private const int PromotionScore = 400;
    private const int VictoryScore = 1000000;
    private const int DefeatScore = -VictoryScore;
    private const int DrawScore = DefeatScore;
    private const int PawnScorePerCellFromBorder = -3;
    private const int QueenScorePerCellFromBorder = 0;
    private const int QueenMovedScore = -100;
    private const int PawnMovedScore = 100;
    private const int ScorePerPawn = 25;
    private const int ScorePerQueen = 100;

    private const int AlphaBetaPruneScore = 1500;

    private static int RateMove(Board board, Move move, PieceColor fromPerspective)
    {
        var fullInfo = board.MoveGenerator.GetMoveFullInfo(move);
        var score = 0;

        var boardSize = postMoveBoard.Size;
        foreach (var pieceOnBoard in postMoveBoard.GetAllPieces())
        {
            score += pieceOnBoard.Piece.Type == PieceType.Pawn ? ScorePerPawn : ScorePerQueen;
            var position = pieceOnBoard.Position;
            var distanceFromBorder = Math.Max(Math.Max(position.X, position.Y),
                Math.Max(boardSize - position.X - 1, boardSize - position.Y - 1));
            score += distanceFromBorder * (pieceOnBoard.Piece.Type == PieceType.Pawn
                ? PawnScorePerCellFromBorder
                : QueenScorePerCellFromBorder);
        }

        var capturedPieces = fullInfo.CapturedPositions
            .Select(preMoveBoard.GetPieceAt)
            .ToArray();

        foreach (var capturedPiece in capturedPieces)
        {
            score += capturedPiece.Type == PieceType.Pawn ? PawnCapturedScore : QueenCapturedScore;
        }

        if (fullInfo.HasBeenPromoted)
        {
            score += PromotionScore;
        }

        score += leadingMove.PieceOnBoard.Piece.Type == PieceType.Queen || fullInfo.HasBeenPromoted
            ? QueenMovedScore
            : PawnMovedScore;

        var gameEndState = postMoveBoard.GetGameEndState();
        switch (gameEndState)
        {
            case GameEndState.Draw:
                score += DrawScore;
                break;
            case GameEndState.WhiteWin:
                score += fullInfo.Piece.Color == PieceColor.White ? VictoryScore : DefeatScore;
                break;
            case GameEndState.BlackWin:
                score += fullInfo.Piece.Color == PieceColor.Black ? VictoryScore : DefeatScore;
                break;
        }

        return score;
    }

    private static void BuildMoveTree(BoardMovesTreeNode node, int depth, int maxDepth,
        bool useMultithreading = false)
    {
        if (depth > maxDepth)
        {
            return;
        }

        var sign = depth % 2 == 1 ? -1 : 1;

        lock (node.Board)
        {
            var nextBoards = GenerateNextMoves(node.Board);
            foreach (var (newBoard, move) in nextBoards)
            {
                var leaf = new BoardMovesTreeNode
                {
                    Board = newBoard,
                    LeadingMove = move,
                    Parent = node,
                    CummulativeScore = RateMove(node.Board, newBoard, move) * sign + node.CummulativeScore
                };

                node.Children.Add(leaf);
            }
        }

        if (useMultithreading)
        {
            var threads = new List<Thread>();
            foreach (var child in node.Children)
            {
                var thread = new Thread(() => BuildMoveTree(child, depth + 1, maxDepth));
                thread.Start();
                threads.Add(thread);
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }
        }
        else
        {
            foreach (var child in node.Children)
            {
                if (Math.Abs(child.CummulativeScore) > AlphaBetaPruneScore)
                {
                    continue;
                }

                BuildMoveTree(child, depth + 1, maxDepth);
            }
        }
    }

    private static BoardMovesTreeNode BuildMoveTree(Board board, int maxDepth, bool useMultithreading)
    {
        var root = new BoardMovesTreeNode
        {
            Board = board
        };
        BuildMoveTree(root, 0, maxDepth, useMultithreading);
        return root;
    }

    private static Move GetBestMove(BoardMovesTreeNode root)
    {
        int GetBestScore(BoardMovesTreeNode node)
        {
            if (node.Children.Count == 0)
            {
                return node.CummulativeScore;
            }

            return node.Children.Select(GetBestScore).Max();
        }

        var maxScore = int.MinValue;
        BoardMovesTreeNode bestChildNode = null!;
        foreach (var child in root.Children)
        {
            var childMaxScore = GetBestScore(child);
            if (childMaxScore <= maxScore)
            {
                continue;
            }

            maxScore = childMaxScore;
            bestChildNode = child;
        }

        Console.WriteLine($"Selected move with score: {bestChildNode.CummulativeScore}");
        return bestChildNode.LeadingMove!;
    }

    public static Move GetBestMove(Board board, int maxDepth,
        bool useMultithreading)
    {
        var root = BuildMoveTree(board, maxDepth, useMultithreading);
        return GetBestMove(root);
    }
}