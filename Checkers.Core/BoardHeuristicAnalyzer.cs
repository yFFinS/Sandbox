using System.Drawing;

namespace Checkers.Core;

public class HeuristicAnalyzerConfig
{
    public const int VictoryScore = int.MaxValue;
    public const int DefeatScore = -VictoryScore;
    public const int DrawScore = DefeatScore / 2;
    public const int MaxRandomScoreExclusive = 10;

    public int PawnScorePerCellFromBorder { get; set; } = -5;
    public int QueenScorePerCellFromBorder { get; set; } = 5;
    public int PromotionCellFreeScore { get; set; } = 20;
    public int PawnMovableScore { get; set; } = 60;
    public int QueenMovableScore { get; set; } = 25;
    public int PawnAtBorderScore { get; set; } = 50;
    public int QueenAtBorderScore { get; set; } = 20;
    public int PawnAliveScore { get; set; } = 350;
    public int QueenAliveScore { get; set; } = 1000;
    public int PieceCountDifferenceScore { get; set; } = 50;
    public int DefenderPieceScore { get; set; } = 15;
    public int AttackerPawnScore { get; set; } = 20;
    public int CentralPawnScore { get; set; } = 30;
    public int MainDiagonalPawnScore { get; set; } = 40;
    public int MainDiagonalQueenScore { get; set; } = 70;
    public int DoubleDiagonalPawnScore { get; set; } = 30;
    public int DoubleDiagonalQueenScore { get; set; } = 60;
    public int LonerPawnScore { get; set; } = 50;
    public int LonerQueenScore { get; set; } = 100;
    public int HoleScore { get; set; } = 170;
    public int TriangleScore { get; set; } = 120;
    public int OreoScore { get; set; } = 90;
    public int BridgeScore { get; set; } = 100;
    public int DogScore { get; set; } = 250;
    public int CorneredQueen { get; set; } = 500;
    public int CorneredPawn { get; set; } = 100;
}

public class BoardHeuristicAnalyzer
{
    private readonly Random _random = new(264343821);

    private PieceColor _fromPerspective;
    private readonly HeuristicAnalyzerConfig _config = new();

    public void Configure(Action<HeuristicAnalyzerConfig> configurator)
    {
        configurator(_config);
    }

    public int EvaluateBoard(Board board, PieceColor fromPerspective)
    {
        _fromPerspective = fromPerspective;

        var boardSize = board.Size;
        var pieceOnBoards = board.GetAllPieces().ToArray();

        var score = 0;

        score += EvaluateAlivePieces(pieceOnBoards);
        score += EvaluateSafePieces(boardSize, pieceOnBoards);
        score += EvaluateMovablePieces(board, pieceOnBoards);
        score += EvaluateFreePromotionCells(board);
        score += EvaluateDistanceFromPromotionLines(boardSize, pieceOnBoards);
        score += EvaluateDifferenceInPieceCount(pieceOnBoards);
        score += EvaluateDefenderPieces(boardSize, pieceOnBoards);
        score += EvaluateAttackerPawns(boardSize, pieceOnBoards);
        score += EvaluateCentralPawns(boardSize, pieceOnBoards);
        score += EvaluateMainDiagonalPieces(boardSize, pieceOnBoards);
        score += EvaluateDoubleDiagonalPieces(pieceOnBoards);
        score += EvaluateLonerPieces(board, pieceOnBoards);

        score += EvaluateHoles(board);
        score += EvaluateTriangles(board);
        score += EvaluateOreos(board);
        score += EvaluateBridges(board);
        score += EvaluateDogs(board);
        score += EvaluateCorneredPieces(board);

        score *= 10;
        score += _random.Next(HeuristicAnalyzerConfig.MaxRandomScoreExclusive);

        return score;
    }

    private int EvaluateCorneredPieces(Board board)
    {
        var score = 0;

        var topRightPiece = board.GetPieceAt(new Position(board.Size - 1, 0));
        var bottomLeftPiece = board.GetPieceAt(new Position(0, board.Size - 1));

        if (!topRightPiece.IsEmpty)
        {
            score += MatchPiece(topRightPiece, _config.CorneredPawn, _config.CorneredQueen);
        }

        if (!bottomLeftPiece.IsEmpty)
        {
            score += MatchPiece(bottomLeftPiece, _config.CorneredPawn, _config.CorneredQueen);
        }

        return score;
    }

    private int EvaluateDogs(Board board)
    {
        var score = 0;

        var topRowPiece = board.GetPieceAt(new Position(1, 0));
        if (!topRowPiece.IsEmpty && topRowPiece.Color == PieceColor.Black)
        {
            var doggedPiece = board.GetPieceAt(new Position(0, 1));
            if (!doggedPiece.IsEmpty && doggedPiece.Color == PieceColor.White)
            {
                score += GetSign(PieceColor.Black) * _config.DogScore;
            }
        }

        var boardSize = board.Size;
        var bottomRowPiece = board.GetPieceAt(new Position(boardSize - 2, boardSize - 1));
        if (!bottomRowPiece.IsEmpty && bottomRowPiece.Color == PieceColor.White)
        {
            var doggedPiece = board.GetPieceAt(new Position(boardSize - 1, boardSize - 2));
            if (!doggedPiece.IsEmpty && doggedPiece.Color == PieceColor.Black)
            {
                score += GetSign(PieceColor.White) * _config.DogScore;
            }
        }

        return score;
    }

    private int EvaluateBridges(Board board)
    {
        (bool, PieceColor) TryGetBridgeColor(int x, int y)
        {
            var leftPosition = new Position(x, y);
            var rightPosition = new Position(x + 3, y);

            var leftPiece = board.GetPieceAt(leftPosition);
            if (leftPiece.IsEmpty)
            {
                return (false, PieceColor.Black);
            }

            var rightPiece = board.GetPieceAt(rightPosition);
            return (!rightPiece.IsEmpty && rightPiece.Color == leftPiece.Color, leftPiece.Color);
        }

        var score = 0;
        for (var x = 0; x < board.Size - 4; x += 2)
        {
            for (var y = 0; y < board.Size; y++)
            {
                var (success, color) = TryGetBridgeColor(x, y);
                if (success && (color == PieceColor.White && y >= board.Size / 2 + 1 ||
                                color == PieceColor.Black && y < board.Size / 2 - 1))
                {
                    score += GetSign(color) * _config.BridgeScore;
                }
            }
        }

        return score;
    }

    private int EvaluateOreos(Board board)
    {
        var score = 0;

        for (var y = board.Size - 1; y > board.Size / 2; y--)
        {
            for (var x = 3; x < board.Size - 3; x += 2)
            {
                if (IsTriangle(board, x, y, PieceColor.White))
                {
                    score += GetSign(PieceColor.White) * _config.OreoScore;
                }
            }
        }

        for (var y = 1; y < board.Size / 2; y++)
        {
            for (var x = 3; x < board.Size - 3; x += 2)
            {
                if (IsTriangle(board, x, y, PieceColor.Black))
                {
                    score += GetSign(PieceColor.Black) * _config.OreoScore;
                }
            }
        }

        return score;
    }

    private static readonly int[] OneDimensionalDirections = { -1, 1 };

    private bool IsTriangle(Board board, int x, int y, PieceColor color)
    {
        var pivot = new Position(x, y);
        var pivotPiece = board.GetPieceAt(pivot);
        if (pivotPiece.IsEmpty || pivotPiece.Color != color)
        {
            return false;
        }

        var dy = color == PieceColor.White ? 1 : -1;
        foreach (var dx in OneDimensionalDirections)
        {
            var position = pivot.Offset(dx, dy);
            var piece = board.GetPieceAt(position);
            if (piece.IsEmpty || piece.Color != color)
            {
                return false;
            }
        }

        return true;
    }

    private int EvaluateTriangles(Board board)
    {
        var score = 0;

        for (var y = board.Size - 1; y > board.Size / 2; y--)
        {
            if (IsTriangle(board, board.Size - 3, y, PieceColor.White))
            {
                score += GetSign(PieceColor.White) * _config.TriangleScore;
            }
        }

        for (var y = 1; y < board.Size / 2; y++)
        {
            if (IsTriangle(board, 2, y, PieceColor.Black))
            {
                score += GetSign(PieceColor.Black) * _config.TriangleScore;
            }
        }

        return score;
    }

    private int EvaluateHoles(Board board)
    {
        (bool success, PieceColor color) TryGetHoleOppositeColor(int x, int y)
        {
            var position = new Position(x, y);
            if (!board.GetPieceAt(position).IsEmpty)
            {
                return (false, PieceColor.Black);
            }

            var whites = 0;
            var blacks = 0;
            foreach (var adjacentPosition in GetAdjacentPositions(board, position))
            {
                var piece = board.GetPieceAt(adjacentPosition);
                if (piece.IsEmpty)
                {
                    continue;
                }

                if (piece.Color == PieceColor.White)
                {
                    whites++;
                }
                else
                {
                    blacks++;
                }
            }

            if (whites >= 3)
            {
                return (true, PieceColor.Black);
            }

            if (blacks >= 3)
            {
                return (true, PieceColor.White);
            }

            return (false, PieceColor.Black);
        }

        var score = 0;

        for (var x = 1; x < board.Size - 1; x++)
        {
            for (var y = 1 + x % 2; y < board.Size - 2 + x % 2; y++)
            {
                var (success, color) = TryGetHoleOppositeColor(x, y);
                if (success)
                {
                    score += GetSign(color) * _config.HoleScore;
                }
            }
        }

        return score;
    }

    private int EvaluateMainDiagonalPieces(int boardSize, IEnumerable<PieceOnBoard> pieceOnBoards)
    {
        bool IsOnMainDiagonal(Position position)
        {
            return position.X + position.Y == boardSize - 1;
        }

        var score = 0;
        foreach (var pieceOnBoard in pieceOnBoards)
        {
            if (IsOnMainDiagonal(pieceOnBoard.Position))
            {
                score += MatchPiece(pieceOnBoard.Piece, _config.MainDiagonalPawnScore, _config.MainDiagonalQueenScore);
            }
        }

        return score;
    }

    private int EvaluateDoubleDiagonalPieces(IEnumerable<PieceOnBoard> pieceOnBoards)
    {
        bool IsOnDoubleDiagonal(Position position)
        {
            return Math.Abs(position.X - position.Y) == 1;
        }

        var score = 0;
        foreach (var pieceOnBoard in pieceOnBoards)
        {
            if (IsOnDoubleDiagonal(pieceOnBoard.Position))
            {
                score += MatchPiece(pieceOnBoard.Piece, _config.DoubleDiagonalPawnScore,
                    _config.DoubleDiagonalQueenScore);
            }
        }

        return score;
    }

    private int EvaluateCentralPawns(int boardSize, IEnumerable<PieceOnBoard> pieceOnBoards)
    {
        var centerMin = boardSize / 2 - 2;
        var centerMax = boardSize / 2 + 1;

        bool IsCentralPawn(Position position)
        {
            return position.X >= centerMin
                   && position.X <= centerMax
                   && position.Y >= centerMin
                   && position.Y <= centerMax;
        }

        var score = 0;
        foreach (var pieceOnBoard in pieceOnBoards.Where(p => p.Piece.Type == PieceType.Pawn))
        {
            if (IsCentralPawn(pieceOnBoard.Position))
            {
                score += GetSign(pieceOnBoard.Piece.Color) * _config.CentralPawnScore;
            }
        }

        return score;
    }

    private int EvaluateAttackerPawns(int boardSize, IEnumerable<PieceOnBoard> pieceOnBoards)
    {
        bool IsAttackerPawn(Position position, PieceColor color)
        {
            return position.Y <= 2 && color == PieceColor.White ||
                   position.Y >= boardSize - 3 && color == PieceColor.Black;
        }

        var score = 0;
        foreach (var pieceOnBoard in pieceOnBoards.Where(p => p.Piece.Type == PieceType.Pawn))
        {
            var color = pieceOnBoard.Piece.Color;
            if (IsAttackerPawn(pieceOnBoard.Position, color))
            {
                score += GetSign(color) * _config.AttackerPawnScore;
            }
        }

        return score;
    }

    private static readonly Point[] Directions = { new(-1, -1), new(-1, 1), new(1, -1), new(1, 1) };

    private static IEnumerable<Position> GetAdjacentPositions(Board board, Position position)
    {
        foreach (var direction in Directions)
        {
            var newPosition = position.Offset(direction.X, direction.Y);
            if (board.IsInBounds(newPosition))
            {
                yield return newPosition;
            }
        }
    }

    private static IEnumerable<Position> GetMovablePositions(Board board, PieceOnBoard pieceOnBoard)
    {
        var color = pieceOnBoard.Piece.Color;
        foreach (var adjacentPosition in GetAdjacentPositions(board, pieceOnBoard.Position))
        {
            var direction = pieceOnBoard.Position.DirectionTo(adjacentPosition);
            if (direction.dy == 1 && color == PieceColor.White || direction.dy == -1 && color == PieceColor.Black)
            {
                continue;
            }

            if (board.GetPieceAt(adjacentPosition).IsEmpty)
            {
                yield return adjacentPosition;
            }
        }
    }

    private int EvaluateLonerPieces(Board board, IEnumerable<PieceOnBoard> pieceOnBoards)
    {
        bool IsLonerPiece(Position position)
        {
            return GetAdjacentPositions(board, position).All(pos => board.GetPieceAt(pos).IsEmpty);
        }

        var score = 0;
        foreach (var pieceOnBoard in pieceOnBoards)
        {
            if (IsLonerPiece(pieceOnBoard.Position))
            {
                score += MatchPiece(pieceOnBoard.Piece, _config.LonerPawnScore, _config.LonerQueenScore);
            }
        }

        return score;
    }

    private int GetSign(PieceColor color)
    {
        return color == _fromPerspective ? 1 : -1;
    }

    private int MatchPiece(Piece piece, int pawnValue, int queenValue)
    {
        var sign = GetSign(piece.Color);
        return sign * (piece.Type == PieceType.Pawn ? pawnValue : queenValue);
    }

    private int EvaluateDefenderPieces(int boardSize, IEnumerable<PieceOnBoard> pieceOnBoards)
    {
        bool IsDefenderPiece(Position position, PieceColor color)
        {
            return position.Y <= 1 && color == PieceColor.Black ||
                   position.Y >= boardSize - 2 && color == PieceColor.White;
        }

        var score = 0;
        foreach (var pieceOnBoard in pieceOnBoards)
        {
            var color = pieceOnBoard.Piece.Color;
            if (IsDefenderPiece(pieceOnBoard.Position, color))
            {
                score += GetSign(color) * _config.DefenderPieceScore;
            }
        }

        return score;
    }

    private int EvaluateDifferenceInPieceCount(IReadOnlyCollection<PieceOnBoard> pieceOnBoards)
    {
        var whiteCount = pieceOnBoards.Count(p => p.Piece.Color == PieceColor.White);
        var blackCount = pieceOnBoards.Count - whiteCount;
        return GetSign(_fromPerspective) * (blackCount - whiteCount) * _config.PieceCountDifferenceScore;
    }

    private int EvaluateDistanceFromPromotionLines(int boardSize, IEnumerable<PieceOnBoard> pieceOnBoards)
    {
        var score = 0;
        foreach (var pieceOnBoard in pieceOnBoards)
        {
            var position = pieceOnBoard.Position;
            var distanceFromBorder =
                pieceOnBoard.Piece.Color == PieceColor.Black ? boardSize - 1 - position.Y : position.Y;
            score += distanceFromBorder * MatchPiece(pieceOnBoard.Piece, _config.PawnScorePerCellFromBorder,
                _config.QueenScorePerCellFromBorder);
        }

        return score;
    }

    private int EvaluateFreePromotionCells(Board board)
    {
        var score = 0;

        foreach (var y in new[] { 0, board.Size - 1 })
        {
            for (var x = 1 - y % 2; x < board.Size; x += 2)
            {
                var position = new Position(x, y);
                if (!board.GetPieceAt(position).IsEmpty)
                {
                    continue;
                }

                var color = y == 0 ? PieceColor.White : PieceColor.Black;
                score += GetSign(color) * _config.PromotionCellFreeScore;
            }
        }

        return score;
    }

    private int EvaluateMovablePieces(Board board, IEnumerable<PieceOnBoard> pieceOnBoards)
    {
        bool IsMovable(PieceOnBoard pieceOnBoard)
        {
            return GetMovablePositions(board, pieceOnBoard).Any();
        }

        var score = 0;
        foreach (var pieceOnBoard in pieceOnBoards.Where(IsMovable))
        {
            score += MatchPiece(pieceOnBoard.Piece, _config.PawnMovableScore, _config.QueenMovableScore);
        }

        return score;
    }

    private int EvaluateSafePieces(int boardSize, IEnumerable<PieceOnBoard> pieceOnBoards)
    {
        bool IsAtBorder(Position position)
        {
            return position.X == 0 || position.X == boardSize - 1 || position.Y == 0 || position.Y == boardSize - 1;
        }

        var score = 0;
        foreach (var pieceOnBoard in pieceOnBoards)
        {
            if (IsAtBorder(pieceOnBoard.Position))
            {
                score += MatchPiece(pieceOnBoard.Piece, _config.PawnAtBorderScore, _config.QueenAtBorderScore);
            }
        }

        return score;
    }

    private int EvaluateAlivePieces(IEnumerable<PieceOnBoard> pieceOnBoards)
    {
        var score = 0;
        foreach (var pieceOnBoard in pieceOnBoards)
        {
            score += MatchPiece(pieceOnBoard.Piece, _config.PawnAliveScore, _config.QueenAliveScore);
        }

        return score;
    }

    public int GetGameEndScore(GameEndState gameEndState)
    {
        switch (gameEndState)
        {
            case GameEndState.Draw:
                return HeuristicAnalyzerConfig.DrawScore;
            case GameEndState.WhiteWin:
                return _fromPerspective == PieceColor.White
                    ? HeuristicAnalyzerConfig.VictoryScore
                    : HeuristicAnalyzerConfig.DefeatScore;
            case GameEndState.BlackWin:
                return _fromPerspective == PieceColor.Black
                    ? HeuristicAnalyzerConfig.VictoryScore
                    : HeuristicAnalyzerConfig.DefeatScore;
            default:
                throw new ArgumentOutOfRangeException(nameof(gameEndState), gameEndState, null);
        }
    }
}