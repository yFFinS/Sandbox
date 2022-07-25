using System.Runtime.CompilerServices;

namespace Chess.Core.Solver;

public class BoardHeuristicAnalyzer
{
    private const int RookOnOpenRankBonus = 100;
    private const int RookOnSemiOpenRankBonus = 50;
    private readonly Random _random = new(264343821);

    private HeuristicAnalyzerConfig _config;

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

    public int EvaluateBoard(Board board)
    {
        Span<int> pieceSquares = stackalloc int[32];
        var count = board.GetAllPieces(pieceSquares);
        Span<PieceOnBoard> pieces = stackalloc PieceOnBoard[count];

        for (var i = 0; i < count; i++)
        {
            var square = pieceSquares[i];
            pieces[i] = new PieceOnBoard(board.GetPieceAt(square), square);
        }

        var pawnsCount = board.GetPieceBitboard(PieceType.Pawn).PopCount();

        var score = 0;

        score += EvaluateAlivePieces(board, pieces, pawnsCount);
        score += EvaluateAttackedPieces(board, board.AttackedBitboardIgnoreKing);
        score += EvaluatePinnedPieces(board, board.PinsInfo);
        score += EvaluatePieceSquareTables(board.PieceSquareEvaluator);

        return MatchValue(score, board.ColorToMove) + EvaluateCheckers(board.CheckersBitboard);
    }

    private static int EvaluatePieceSquareTables(PieceSquareEvaluator pieceSquareEvaluator)
    {
        var scores = pieceSquareEvaluator.Scores;
        return scores.Get(PieceColor.White) - scores.Get(PieceColor.Black);
    }

    private int EvaluateCheckers(Bitboard checkers)
    {
        var count = checkers.PopCount();
        if (count == 0)
        {
            return 0;
        }

        return count == 1 ? _config.CheckScore : _config.DoubleCheckScore;
    }

    private int EvaluatePinnedPieces(Board board, ReadOnlyColorIndexer<PinsInfo> pinsInfo)
    {
        var score = 0;

        var whitePins = pinsInfo.Get(PieceColor.White).AllPins;
        var blackPins = pinsInfo.Get(PieceColor.Black).AllPins;

        Span<int> whitePinnedSquares = stackalloc int[8];
        var whiteCount = (board.GetColorBitboard(PieceColor.White) & whitePins).BitScanForwardAll(whitePinnedSquares);
        Span<int> blackPinnedSquares = stackalloc int[8];
        var blackCount = (board.GetColorBitboard(PieceColor.Black) & blackPins).BitScanForwardAll(blackPinnedSquares);

        for (var i = 0; i < whiteCount; i++)
        {
            var pieceType = board.GetPieceTypeAt(whitePinnedSquares[i]);
            score += _config.PiecePinnedScore.Get(pieceType);
        }

        for (var i = 0; i < blackCount; i++)
        {
            var pieceType = board.GetPieceTypeAt(blackPinnedSquares[i]);
            score -= _config.PiecePinnedScore.Get(pieceType);
        }

        return score;
    }

    private int EvaluateAttackedPieces(Board board, ReadOnlyColorIndexer<Bitboard> attackedSquares)
    {
        var whiteAttacked = attackedSquares.Get(PieceColor.White) & board.GetColorBitboard(PieceColor.White);
        var blackAttacked = attackedSquares.Get(PieceColor.Black) & board.GetColorBitboard(PieceColor.Black);

        var score = 0;

        Span<int> whiteAttackedPieces = stackalloc int[16];
        var whiteAttackedCount = whiteAttacked.BitScanForwardAll(whiteAttackedPieces);
        for (var i = 0; i < whiteAttackedCount; i++)
        {
            var attackedType = board.GetPieceTypeAt(whiteAttackedPieces[i]);
            score -= _config.PieceAttackedScore.Get(attackedType);
        }

        Span<int> blackAttackedPieces = stackalloc int[16];
        var blackAttackedCount = blackAttacked.BitScanForwardAll(blackAttackedPieces);
        for (var i = 0; i < blackAttackedCount; i++)
        {
            var attackedType = board.GetPieceTypeAt(blackAttackedPieces[i]);
            score += _config.PieceAttackedScore.Get(attackedType);
        }

        return score;
    }

    private int EvaluateAlivePieces(Board board, ReadOnlySpan<PieceOnBoard> pieces, int pawnsCount)
    {
        var score = 0;

        foreach (var pieceOnBoard in pieces)
        {
            var pieceType = pieceOnBoard.Piece.Type;
            var pieceScore = _config.PieceAliveScore.Get(pieceType);
            pieceScore += GetPieceScoreTweak(board, pawnsCount, pieceOnBoard);
            score += MatchValue(pieceScore, pieceOnBoard.Piece.Color);
        }

        return score;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetPieceScoreTweak(Board board, int pawnsCount, PieceOnBoard pieceOnBoard)
    {
        var pieceType = pieceOnBoard.Piece.Type;

        var square = pieceOnBoard.Square;
        if (pieceType == PieceType.Pawn)
        {
            var tweak = 0;

            var pawns = board.GetPieceBitboard(PieceType.Pawn);
            var allyPawns = pawns & board.GetColorBitboard(pieceOnBoard.Piece.Color);
            var enemyPawns = pawns & board.GetColorBitboard(pieceOnBoard.Piece.Color.Opposite());

            // Соседние клетки совпадают с теми, на которые может сходить король
            var isIsolated = (allyPawns & BitboardLookups.KingMoves[square]) == 0;

            if (isIsolated)
            {
                tweak += PawnIsolatedPenalty;
            }

            var passedBonus = 0;
            var pawnBitboard = Bitboard.WithSetBit(square);
            if (pieceOnBoard.Piece.Color == PieceColor.White)
            {
                var passedCheck = ((pawnBitboard >> 8) | (pawnBitboard >> 16) | (pawnBitboard >> 24) | (pawnBitboard >> 32) | (pawnBitboard >> 40)) &
                                  (~Rank.R1 | ~Rank.R2 | ~Rank.R3 | ~Rank.R4);
                if ((passedCheck & enemyPawns) == 0)
                {
                    passedBonus = BlackPawnPassedRank[7 - square / 8];
                }
            }
            else
            {
                var passedCheck = ((pawnBitboard << 8) | (pawnBitboard << 16) | (pawnBitboard << 24) | (pawnBitboard << 32) | (pawnBitboard << 40)) &
                                  (~Rank.R5 | ~Rank.R6 | ~Rank.R7 | ~Rank.R8);
                if ((passedCheck & enemyPawns) == 0)
                {
                    passedBonus = BlackPawnPassedRank[square / 8];
                }
            }

            if (board.IsEndGame)
            {
                tweak += passedBonus * 2;
            }
            else
            {
                tweak += passedBonus;
            }

            return tweak;
        }

        if (pieceType == PieceType.Knight)
        {
            return (pawnsCount - 10) * 6;
        }

        if (pieceType == PieceType.Bishop)
        {
            return (10 - pawnsCount) * 6;
        }

        if (pieceType != PieceType.Rook)
        {
            return 0;
        }

        var rank = BitboardLookups.Ranks[square / 8];
        var pawnsOnRookRank = board.GetPieceBitboard(PieceType.Pawn) & rank;

        if (pawnsOnRookRank == 0)
        {
            return RookOnOpenRankBonus;
        }

        if ((pawnsOnRookRank & board.GetColorBitboard(pieceOnBoard.Piece.Color)) == 0)
        {
            return RookOnSemiOpenRankBonus;
        }

        return 0;
    }

    private const int PawnIsolatedPenalty = -20;
    private static readonly int[] BlackPawnPassedRank = { 0, 5, 10, 20, 40, 80, 160, 0 };

    private static int MatchValue(int value, PieceColor color)
    {
        return color == PieceColor.White ? value : -value;
    }
}