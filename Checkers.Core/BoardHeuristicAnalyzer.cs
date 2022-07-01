using System.Drawing;
using System.Runtime.CompilerServices;

namespace Checkers.Core;

public class BoardHeuristicAnalyzer
{
    private readonly Random _random = new(264343821);

    private PieceColor _fromPerspective;
    private HeuristicAnalyzerConfig _config;

    private readonly PieceOnBoard[] _pieceBuffer = new PieceOnBoard[24];

    public BoardHeuristicAnalyzer()
    {
        _config = new HeuristicAnalyzerConfig();
    }

    public BoardHeuristicAnalyzer(HeuristicAnalyzerConfig config)
    {
        _config = config.Copy();
    }

    public void Configure(HeuristicAnalyzerConfig config)
    {
        _config = config.Copy();
    }

    public void Configure(Action<HeuristicAnalyzerConfig> configurator)
    {
        configurator(_config);
        _config = _config.Copy();
    }

    public int EvaluateBoard(Board board, in PieceColor fromPerspective)
    {
        _fromPerspective = fromPerspective;

        var boardSize = board.Size;
        var count = board.GetAllPiecesNonAlloc(_pieceBuffer);

        var score = 0;

        score += EvaluateAlivePieces(_pieceBuffer.Take(count));
        score += EvaluateSafePieces(boardSize, _pieceBuffer.Take(count));
        score += EvaluateMovablePieces(board, _pieceBuffer.Take(count));
        score += EvaluateFreePromotionCells(board);
        score += EvaluateDistanceFromPromotionLines(boardSize, _pieceBuffer.Take(count));
        score += EvaluateDifferenceInPieceCount(_pieceBuffer.Take(count));
        score += EvaluateDefenderPieces(boardSize, _pieceBuffer.Take(count));
        score += EvaluateAttackerPawns(boardSize, _pieceBuffer.Take(count));
        score += EvaluateCentralPawns(boardSize, _pieceBuffer.Take(count));
        score += EvaluateMainDiagonalPieces(boardSize, _pieceBuffer.Take(count));
        score += EvaluateDoubleDiagonalPieces(_pieceBuffer.Take(count));
        score += EvaluateLonerPieces(board, _pieceBuffer.Take(count));

        score += EvaluateHoles(board);
        score += EvaluateTriangles(board);
        score += EvaluateOreos(board);
        score += EvaluateBridges(board);
        score += EvaluateDogs(board);
        score += EvaluateCorneredPieces(board);

        score += EvaluateStalemate(board, score);

        score *= 10;
        score += _random.Next(HeuristicAnalyzerConfig.MaxRandomScoreExclusive);

        return score;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static int EvaluateStalemate(Board board, int score)
    {
        var beforeStalemateTurns = Board.MaxStalemateTurns - board.StalemateTurns;
        var beforeMaxTurnTurns = Board.MaxTurns - board.TurnCount;

        var sign = Math.Sign(score);
        return sign * (beforeStalemateTurns * HeuristicAnalyzerConfig.PerTurnBeforeStalemateScore +
                       beforeMaxTurnTurns * HeuristicAnalyzerConfig.PerTurnBeforeMaxTurnsScore);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int EvaluateCorneredPieces(Board board)
    {
        var score = 0;

        var topRightPiece = board.GetPieceAt(board.Size - 1, 0);
        var bottomLeftPiece = board.GetPieceAt(0, board.Size - 1);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int EvaluateDogs(Board board)
    {
        var score = 0;

        var topRowPiece = board.GetPieceAt(1, 0);
        if (!topRowPiece.IsEmpty && topRowPiece.Color == PieceColor.Black)
        {
            var doggedPiece = board.GetPieceAt(0, 1);
            if (!doggedPiece.IsEmpty && doggedPiece.Color == PieceColor.White && doggedPiece.Type == PieceType.Pawn)
            {
                score += GetSign(PieceColor.Black) * _config.DogScore;
            }
        }

        var boardSize = board.Size;
        var bottomRowPiece = board.GetPieceAt(boardSize - 2, boardSize - 1);
        if (!bottomRowPiece.IsEmpty && bottomRowPiece.Color == PieceColor.White)
        {
            var doggedPiece = board.GetPieceAt(boardSize - 1, boardSize - 2);
            if (!doggedPiece.IsEmpty && doggedPiece.Color == PieceColor.Black && doggedPiece.Type == PieceType.Pawn)
            {
                score += GetSign(PieceColor.White) * _config.DogScore;
            }
        }

        return score;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool IsBridge(in Piece leftPiece, in Piece rightPiece, in PieceColor color)
    {
        if (leftPiece.IsEmpty || rightPiece.IsEmpty)
        {
            return false;
        }

        if (leftPiece.Type == PieceType.Pawn || rightPiece.Type == PieceType.Pawn)
        {
            return false;
        }

        return leftPiece.Color == color && rightPiece.Color == color;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int EvaluateBridges(Board board)
    {
        var score = 0;

        var bottomLeftPiece = board.GetPieceAt(2, board.Size - 1);
        var bottomRightPiece = board.GetPieceAt(board.Size - 2, board.Size - 1);

        if (IsBridge(bottomLeftPiece, bottomRightPiece, PieceColor.White))
        {
            score += GetSign(PieceColor.White) * _config.BridgeScore;
        }

        var topLeftPiece = board.GetPieceAt(1, 0);
        var topRightPiece = board.GetPieceAt(board.Size - 3, 0);

        if (IsBridge(topLeftPiece, topRightPiece, PieceColor.Black))
        {
            score += GetSign(PieceColor.Black) * _config.BridgeScore;
        }

        return score;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int EvaluateOreos(Board board)
    {
        var score = 0;

        if (IsTriangle(board, 3, board.Size - 2, PieceColor.White))
        {
            score += GetSign(PieceColor.White) * _config.OreoScore;
        }

        if (IsTriangle(board, board.Size - 4, 1, PieceColor.Black))
        {
            score += GetSign(PieceColor.Black) * _config.OreoScore;
        }

        return score;
    }

    private static readonly int[] OneDimensionalDirections = { -1, 1 };

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool IsTriangle(Board board, int x, int y, in PieceColor color)
    {
        var pivotPiece = board.GetPieceAt(x, y);
        if (pivotPiece.IsEmpty || pivotPiece.Color != color || pivotPiece.Type != PieceType.Pawn)
        {
            return false;
        }

        var dy = color == PieceColor.White ? 1 : -1;
        foreach (var dx in OneDimensionalDirections)
        {
            var piece = board.GetPieceAt(x + dx, y + dy);
            if (piece.IsEmpty || piece.Color != color || piece.Type != PieceType.Pawn)
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int EvaluateTriangles(Board board)
    {
        var score = 0;

        if (IsTriangle(board, board.Size - 3, board.Size - 2, PieceColor.White))
        {
            score += GetSign(PieceColor.White) * _config.TriangleScore;
        }

        if (IsTriangle(board, 2, 1, PieceColor.Black))
        {
            score += GetSign(PieceColor.Black) * _config.TriangleScore;
        }

        return score;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private PieceColor? TryGetHoleOppositeColor(Board board, int x, int y)
    {
        var position = new Position(x, y);
        if (!board.IsEmpty(position))
        {
            return null;
        }

        var whites = 0;
        var blacks = 0;

        var count = GetAdjacentPositionNonAlloc(board, position, _positionBuffer);
        for (var i = 0; i < count; i++)
        {
            var adjacentPosition = _positionBuffer[i];
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
            return PieceColor.Black;
        }

        if (blacks >= 3)
        {
            return PieceColor.White;
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int EvaluateHoles(Board board)
    {
        var score = 0;

        for (var x = 1; x < board.Size - 1; x++)
        {
            for (var y = 1 + x % 2; y < board.Size - 2 + x % 2; y++)
            {
                var color = TryGetHoleOppositeColor(board, x, y);
                if (color.HasValue)
                {
                    score += GetSign(color.Value) * _config.HoleScore;
                }
            }
        }

        return score;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool IsOnMainDiagonal(int boardSize, in Position position)
    {
        return position.X + position.Y == boardSize - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int EvaluateMainDiagonalPieces(int boardSize, IEnumerable<PieceOnBoard> pieceOnBoards)
    {
        var score = 0;
        foreach (var pieceOnBoard in pieceOnBoards)
        {
            if (IsOnMainDiagonal(boardSize, pieceOnBoard.Position))
            {
                score += MatchPiece(pieceOnBoard.Piece, _config.MainDiagonalPawnScore, _config.MainDiagonalQueenScore);
            }
        }

        return score;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool IsOnDoubleDiagonal(in Position position)
    {
        return Math.Abs(position.X - position.Y) == 1;
    }

    private int EvaluateDoubleDiagonalPieces(IEnumerable<PieceOnBoard> pieceOnBoards)
    {
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


    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool IsCentralPawn(int centerMin, int centerMax, in Position position)
    {
        return position.X >= centerMin && position.X <= centerMax && position.Y >= centerMin && position.Y <= centerMax;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int EvaluateCentralPawns(int boardSize, IEnumerable<PieceOnBoard> pieceOnBoards)
    {
        var centerMin = boardSize / 2 - 2;
        var centerMax = boardSize / 2 + 1;

        var score = 0;
        foreach (var pieceOnBoard in pieceOnBoards)
        {
            if (pieceOnBoard.Piece.Type == PieceType.Pawn && IsCentralPawn(centerMin, centerMax, pieceOnBoard.Position))
            {
                score += GetSign(pieceOnBoard.Piece.Color) * _config.CentralPawnScore;
            }
        }

        return score;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool IsAttackerPawn(int boardSize, in Position position, in PieceColor color)
    {
        return position.Y <= 2 && color == PieceColor.White ||
               position.Y >= boardSize - 3 && color == PieceColor.Black;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int EvaluateAttackerPawns(int boardSize, IEnumerable<PieceOnBoard> pieceOnBoards)
    {
        var score = 0;
        foreach (var pieceOnBoard in pieceOnBoards)
        {
            if (pieceOnBoard.Piece.Type != PieceType.Pawn)
            {
                continue;
            }

            var color = pieceOnBoard.Piece.Color;
            if (IsAttackerPawn(boardSize, pieceOnBoard.Position, color))
            {
                score += GetSign(color) * _config.AttackerPawnScore;
            }
        }

        return score;
    }

    private static readonly Point[] Directions = { new(-1, -1), new(-1, 1), new(1, -1), new(1, 1) };

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static int GetAdjacentPositionNonAlloc(Board board, Position position, IList<Position> buffer)
    {
        var bufferIndex = 0;
        foreach (var direction in Directions)
        {
            var newPosition = new Position(position.X + direction.X, position.Y + direction.Y);
            if (board.IsInBounds(newPosition))
            {
                buffer[bufferIndex++] = newPosition;
            }
        }

        return bufferIndex;
    }

    private readonly Position[] _positionBuffer = new Position[4];

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static int GetMovablePositionsNonAlloc(Board board, PieceOnBoard pieceOnBoard, IList<Position>? buffer)
    {
        var color = pieceOnBoard.Piece.Color;
        var position = pieceOnBoard.Position;

        var bufferIndex = 0;
        foreach (var direction in Directions)
        {
            if (direction.Y == 1 && color == PieceColor.White || direction.Y == -1 && color == PieceColor.Black)
            {
                continue;
            }

            var newPosition = new Position(position.X + direction.X, position.Y + direction.Y);
            if (!board.IsInBounds(newPosition))
            {
                continue;
            }

            if (board.IsEmpty(newPosition))
            {
                if (buffer is not null)
                {
                    buffer[bufferIndex] = newPosition;
                }

                bufferIndex++;
            }
        }

        return bufferIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private bool IsLonerPiece(Board board, in Position position)
    {
        var count = GetAdjacentPositionNonAlloc(board, position, _positionBuffer);
        for (var i = 0; i < count; i++)
        {
            if (!board.IsEmpty(_positionBuffer[i]))
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int EvaluateLonerPieces(Board board, IEnumerable<PieceOnBoard> pieceOnBoards)
    {
        var score = 0;
        foreach (var pieceOnBoard in pieceOnBoards)
        {
            if (IsLonerPiece(board, pieceOnBoard.Position))
            {
                score += MatchPiece(pieceOnBoard.Piece, _config.LonerPawnScore, _config.LonerQueenScore);
            }
        }

        return score;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int GetSign(in PieceColor color)
    {
        return color == _fromPerspective ? 1 : -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int MatchPiece(in Piece piece, int pawnValue, int queenValue)
    {
        var sign = GetSign(piece.Color);
        return sign * (piece.Type == PieceType.Pawn ? pawnValue : queenValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool IsDefenderPiece(int boardSize, in Position position, in PieceColor color)
    {
        return position.Y <= 1 && color == PieceColor.Black ||
               position.Y >= boardSize - 2 && color == PieceColor.White;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int EvaluateDefenderPieces(int boardSize, IEnumerable<PieceOnBoard> pieceOnBoards)
    {
        var score = 0;
        foreach (var pieceOnBoard in pieceOnBoards)
        {
            var color = pieceOnBoard.Piece.Color;
            if (IsDefenderPiece(boardSize, pieceOnBoard.Position, color))
            {
                score += GetSign(color) * _config.DefenderPieceScore;
            }
        }

        return score;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int EvaluateDifferenceInPieceCount(IEnumerable<PieceOnBoard> pieceOnBoards)
    {
        var whitePieces = 0;
        var blackPieces = 0;
        foreach (var pieceOnBoard in pieceOnBoards)
        {
            if (pieceOnBoard.Piece.Color == PieceColor.White)
            {
                whitePieces++;
            }
            else
            {
                blackPieces++;
            }
        }

        var difference = blackPieces - whitePieces;
        if (_fromPerspective == PieceColor.White)
        {
            difference *= -1;
        }

        return difference * _config.PieceCountDifferenceScore;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int EvaluateDistanceFromPromotionLines(int boardSize, IEnumerable<PieceOnBoard> pieceOnBoards)
    {
        var score = 0;
        foreach (var pieceOnBoard in pieceOnBoards)
        {
            var position = pieceOnBoard.Position;
            var distanceFromPromotion =
                pieceOnBoard.Piece.Color == PieceColor.Black ? boardSize - 1 - position.Y : position.Y;
            score += distanceFromPromotion * MatchPiece(pieceOnBoard.Piece, _config.PawnScorePerCellFromBorder,
                _config.QueenScorePerCellFromBorder);
        }

        return score;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int EvaluateFreePromotionCells(Board board)
    {
        var score = 0;

        for (var i = 0; i < 2; i++)
        {
            var y = i == 0 ? 0 : board.Size - 1;
            for (var x = 1 - y % 2; x < board.Size; x += 2)
            {
                if (!board.IsEmpty(x, y))
                {
                    continue;
                }

                var color = y == 0 ? PieceColor.White : PieceColor.Black;
                score += GetSign(color) * _config.PromotionCellFreeScore;
            }
        }

        return score;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool IsMovable(Board board, in PieceOnBoard pieceOnBoard)
    {
        var count = GetMovablePositionsNonAlloc(board, pieceOnBoard, null);
        return count > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int EvaluateMovablePieces(Board board, IEnumerable<PieceOnBoard> pieceOnBoards)
    {
        var score = 0;
        foreach (var pieceOnBoard in pieceOnBoards)
        {
            if (IsMovable(board, pieceOnBoard))
            {
                score += MatchPiece(pieceOnBoard.Piece, _config.PawnMovableScore, _config.QueenMovableScore);
            }
        }

        return score;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool IsAtBorder(int boardSize, in Position position)
    {
        return position.X == 0 || position.X == boardSize - 1 || position.Y == 0 || position.Y == boardSize - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int EvaluateSafePieces(int boardSize, IEnumerable<PieceOnBoard> pieceOnBoards)
    {
        var score = 0;
        foreach (var pieceOnBoard in pieceOnBoards)
        {
            if (IsAtBorder(boardSize, pieceOnBoard.Position))
            {
                score += MatchPiece(pieceOnBoard.Piece, _config.PawnAtBorderScore, _config.QueenAtBorderScore);
            }
        }

        return score;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int EvaluateAlivePieces(IEnumerable<PieceOnBoard> pieceOnBoards)
    {
        var score = 0;
        foreach (var pieceOnBoard in pieceOnBoards)
        {
            score += MatchPiece(pieceOnBoard.Piece, _config.PawnAliveScore, _config.QueenAliveScore);
        }

        return score;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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