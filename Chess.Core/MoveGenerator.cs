using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Chess.Core;

public class MoveGenerator
{
    private readonly Board _board;
    private readonly Move[] _moves = new Move[256];

    public MoveGenerator(Board board)
    {
        _board = board;
    }

    public Move[] GetLegalMoves(bool capturesOnly = false)
    {
        return GenerateLegalMoves(capturesOnly).ToArray();
    }

    public int GetDetailedLegalMoves(Span<DetailedMove> destination, bool capturesOnly = false)
    {
        var moves = GenerateLegalMoves(capturesOnly);
        for (var i = 0; i < moves.Length; i++)
        {
            destination[i] = _board.GetDetailedMove(moves[i]);
        }

        return moves.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard GenerateCheckersBitboard()
    {
        var checkers = Bitboard.Empty;

        var occupancy = _board.OccupationBitboard;
        var king = _board.GetKingSquare(_board.ColorToMove);
        var them = _board.ColorToMove.Opposite();

        checkers |= BitboardLookups.KnightAttackedSquares[king] & _board.GetPieceBitboard(PieceType.Knight);
        checkers |= BitboardLookups.PawnAttackedSquares[(int)them][king] &
                    _board.GetPieceBitboard(PieceType.Pawn);

        var diagKingMoves = GetBishopMoves(king, occupancy);
        var orthKingMoves = GetRookMoves(king, occupancy);

        checkers |= _board.GetPieceBitboard(PieceType.Bishop) & diagKingMoves;
        checkers |= _board.GetPieceBitboard(PieceType.Rook) & orthKingMoves;
        checkers |= _board.GetPieceBitboard(PieceType.Queen) & (diagKingMoves | orthKingMoves);

        return checkers & _board.GetColorBitboard(them);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Bitboard GetQueenMoves(int queen)
    {
        return GetBishopMoves(queen) | GetRookMoves(queen);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Bitboard GetQueenMoves(int queen, Bitboard occupancy)
    {
        return GetBishopMoves(queen, occupancy) | GetRookMoves(queen, occupancy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Bitboard GetRookMoves(int rook)
    {
        var occupancy = _board.OccupationBitboard;
        return GetRookMoves(rook, occupancy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Bitboard GetRookMoves(int rook, Bitboard occupancy)
    {
        return BitboardLookups.GetFileMoves(rook, occupancy) | BitboardLookups.GetRankMoves(rook, occupancy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Bitboard GetBishopMoves(int bishop, Bitboard occupancy)
    {
        return BitboardLookups.GetDiagonalMoves(bishop, occupancy) |
               BitboardLookups.GetAntiDiagonalMoves(bishop, occupancy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Bitboard GetBishopMoves(int bishop)
    {
        var occupancy = _board.OccupationBitboard;
        return GetBishopMoves(bishop, occupancy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PinsInfo GeneratePinsInfo(PieceColor us)
    {
        var diagonalPinnedMoves = Bitboard.Empty;
        var orthogonalPinnedMoves = Bitboard.Empty;
        var occupancy = _board.OccupationBitboard;

        var king = _board.GetKingSquare(us);

        var diagonalPins = Bitboard.Empty;
        var orthogonalPins = Bitboard.Empty;

        var them = us.Opposite();

        var enemies = _board.GetColorBitboard(them);

        var diagKingMoves = BitboardLookups.GetDiagonalMoves(king, enemies);
        var antiDiagKingMoves = BitboardLookups.GetAntiDiagonalMoves(king, enemies);
        var fileKingMoves = BitboardLookups.GetFileMoves(king, enemies);
        var rankKingMoves = BitboardLookups.GetRankMoves(king, enemies);

        Span<int> diagSliders = stackalloc int[2];
        Span<int> antiDiagSliders = stackalloc int[2];
        Span<int> fileSliders = stackalloc int[2];
        Span<int> rankSliders = stackalloc int[2];

        var realDiagSliders = _board.GetPieceBitboard(PieceType.Bishop) | _board.GetPieceBitboard(PieceType.Queen);
        var realOrthSliders = _board.GetPieceBitboard(PieceType.Rook) | _board.GetPieceBitboard(PieceType.Queen);

        var diagSlidersBitboard = enemies & diagKingMoves & realDiagSliders;
        var antiDiagSlidersBitboard = enemies & antiDiagKingMoves & realDiagSliders;
        var fileSlidersBitboard = enemies & fileKingMoves & realOrthSliders;
        var rankSlidersBitboard = enemies & rankKingMoves & realOrthSliders;

        var diagSlidersCount = diagSlidersBitboard.BitScanForwardAll(diagSliders);
        var antiDiagSlidersCount = antiDiagSlidersBitboard.BitScanForwardAll(antiDiagSliders);
        var fileSlidersCount = fileSlidersBitboard.BitScanForwardAll(fileSliders);
        var rankSlidersCount = rankSlidersBitboard.BitScanForwardAll(rankSliders);

        diagKingMoves = BitboardLookups.GetDiagonalMoves(king, occupancy);
        antiDiagKingMoves = BitboardLookups.GetAntiDiagonalMoves(king, occupancy);
        fileKingMoves = BitboardLookups.GetFileMoves(king, occupancy);
        rankKingMoves = BitboardLookups.GetRankMoves(king, occupancy);

        for (var i = 0; i < diagSlidersCount; i++)
        {
            var sliderAttacks = BitboardLookups.GetDiagonalMoves(diagSliders[i], occupancy);
            var pin = sliderAttacks & diagKingMoves;
            if (pin != 0)
            {
                diagonalPins |= pin;
                diagonalPinnedMoves |= sliderAttacks | diagKingMoves;
                diagonalPinnedMoves.SetAt(diagSliders[i]);
            }
        }

        for (var i = 0; i < antiDiagSlidersCount; i++)
        {
            var sliderAttacks = BitboardLookups.GetAntiDiagonalMoves(antiDiagSliders[i], occupancy);
            var pin = sliderAttacks & antiDiagKingMoves;
            if (pin != 0)
            {
                diagonalPins |= pin;
                diagonalPinnedMoves |= sliderAttacks | antiDiagKingMoves;
                diagonalPinnedMoves.SetAt(antiDiagSliders[i]);
            }
        }

        for (var i = 0; i < fileSlidersCount; i++)
        {
            var sliderAttacks = BitboardLookups.GetFileMoves(fileSliders[i], occupancy);
            var pin = sliderAttacks & fileKingMoves;
            if (pin != 0)
            {
                orthogonalPins |= pin;
                orthogonalPinnedMoves |= sliderAttacks | fileKingMoves;
                orthogonalPinnedMoves.SetAt(fileSliders[i]);
            }
        }

        for (var i = 0; i < rankSlidersCount; i++)
        {
            var sliderAttacks = BitboardLookups.GetRankMoves(rankSliders[i], occupancy);
            var pin = sliderAttacks & rankKingMoves;
            if (pin != 0)
            {
                orthogonalPins |= pin;
                orthogonalPinnedMoves |= sliderAttacks | rankKingMoves;
                orthogonalPinnedMoves.SetAt(rankSliders[i]);
            }
        }

        return new PinsInfo
        {
            DiagonalPins = diagonalPins,
            OrthogonalPins = orthogonalPins,
            DiagonalPinnedMoves = diagonalPinnedMoves,
            OrthogonalPinnedMoves = orthogonalPinnedMoves
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Bitboard white, Bitboard black) GenerateAttackedByBitboardIgnoreKing()
    {
        var whiteAttacked = Bitboard.Empty;
        var blackAttacked = Bitboard.Empty;

        var blackOccupancy = _board.OccupationBitboard;
        blackOccupancy.ResetAt(_board.GetKingSquare(PieceColor.White));
        var whiteOccupancy = _board.OccupationBitboard;
        whiteOccupancy.ResetAt(_board.GetKingSquare(PieceColor.Black));

        Span<int> pieces = stackalloc int[32];
        var count = _board.GetAllPieces(pieces);

        for (var i = 0; i < count; i++)
        {
            var square = pieces[i];
            var pieceType = _board.GetPieceTypeAt(square);
            var color = _board.GetColorAt(square);

            var occupancy = color == PieceColor.Black ? blackOccupancy : whiteOccupancy;
            var attacked = pieceType switch
            {
                PieceType.Pawn => BitboardLookups.PawnAttacks[(int)color][square],
                PieceType.Knight => BitboardLookups.KnightMoves[square],
                PieceType.King => BitboardLookups.KingMoves[square],
                PieceType.Bishop => GetBishopMoves(square, occupancy),
                PieceType.Rook => GetRookMoves(square, occupancy),
                PieceType.Queen => GetQueenMoves(square, occupancy),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (color == PieceColor.Black)
            {
                blackAttacked |= attacked;
            }
            else
            {
                whiteAttacked |= attacked;
            }
        }

        return (whiteAttacked, blackAttacked);
    }

    private ReadOnlySpan<Move> GenerateLegalMoves(bool capturesOnly = false)
    {
        var checkers = _board.CheckersBitboard;
        var checkersCount = checkers.PopCount();

        var end = 0;

        var color = _board.ColorToMove;
        if (checkersCount > 1)
        {
            var kingPosition = _board.GetKingSquare(color);
            end = GenerateKingMoves(_moves, 0, kingPosition, capturesOnly);
            return _moves.AsSpan(..end);
        }

        var pushMask = Bitboard.Filled;
        var captureMask = Bitboard.Filled;

        if (checkersCount == 1)
        {
            var kingPosition = _board.GetKingSquare(color);
            captureMask = checkers;
            var checker = checkers.BitScanForward();
            pushMask = GeneratePushMaskFromChecker(checker, kingPosition);
        }

        var free = _board.FreeBitboard;
        var pawns = _board.GetPieceBitboard(PieceType.Pawn) & _board.GetColorBitboard(color);
        var canDoublePushRank = color == PieceColor.Black ? 1 : 6;

        Bitboard pawnPushMask;
        if (color == PieceColor.White)
        {
            var pawnsSinglePushMask = (pawns >> 8) & free;
            var pawnsDoublePushMask = ((pawns & BitboardLookups.Ranks[canDoublePushRank]) >> 8) & free;
            pawnsDoublePushMask = (pawnsDoublePushMask >> 8) & free;
            pawnPushMask = pushMask & (pawnsSinglePushMask | pawnsDoublePushMask);
        }
        else
        {
            var pawnsSinglePushMask = (pawns << 8) & free;
            var pawnsDoublePushMask = ((pawns & BitboardLookups.Ranks[canDoublePushRank]) << 8) & free;
            pawnsDoublePushMask = (pawnsDoublePushMask << 8) & free;
            pawnPushMask = pushMask & (pawnsSinglePushMask | pawnsDoublePushMask);
        }


        Span<int> pieces = stackalloc int[32];
        var count = _board.GetAllPieces(pieces, color);

        for (var i = 0; i < count; i++)
        {
            var square = pieces[i];
            var pieceType = _board.GetPieceTypeAt(square);

            switch (pieceType)
            {
                case PieceType.Pawn:
                    end = GeneratePawnMoves(_moves, end, square, pawnPushMask, captureMask, capturesOnly);
                    break;
                case PieceType.Knight:
                    end = GenerateKnightMoves(_moves, end, square, pushMask, captureMask, capturesOnly);
                    break;
                case PieceType.Bishop:
                    end = GenerateBishopMoves(_moves, end, square, pushMask, captureMask, capturesOnly);
                    break;
                case PieceType.Rook:
                    end = GenerateRookMoves(_moves, end, square, pushMask, captureMask, capturesOnly);
                    break;
                case PieceType.Queen:
                    end = GenerateQueenMoves(_moves, end, square, pushMask, captureMask, capturesOnly);
                    break;
                case PieceType.King:
                    end = GenerateKingMoves(_moves, end, square, capturesOnly);
                    break;
                default:
                    Debug.Assert(false);
                    throw new ArgumentOutOfRangeException();
            }
        }

        return _moves.AsSpan(..end);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Bitboard GeneratePushMaskFromChecker(int checker, int king)
    {
        return _board.GetPieceTypeAt(checker) is PieceType.Bishop or PieceType.Rook or PieceType.Queen
            ? BitboardLookups.InBetween[checker][king]
            : Bitboard.Empty;
    }

    private static readonly Bitboard[] CastleKingsideShouldNotBeAttacked =
    {
        Bitboard.FromSetBits(new[] { 4, 5, 6 }),
        Bitboard.FromSetBits(new[] { 60, 61, 62 })
    };

    private static readonly Bitboard[] CastleQueensideShouldNotBeAttacked =
    {
        Bitboard.FromSetBits(new[] { 2, 3, 4 }),
        Bitboard.FromSetBits(new[] { 58, 59, 60 })
    };

    private static readonly Bitboard[] CastleOccupationKingside =
    {
        Bitboard.FromSetBits(new[] { 5, 6 }),
        Bitboard.FromSetBits(new[] { 61, 62 }),
    };

    private static readonly Bitboard[] CastleOccupationQueenside =
    {
        Bitboard.FromSetBits(new[] { 1, 2, 3 }),
        Bitboard.FromSetBits(new[] { 57, 58, 59 }),
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GenerateKingMoves(Span<Move> destination, int destinationStart, int position, bool capturesOnly = false)
    {
        var attacked = _board.AttackedBitboardIgnoreKing.Get(_board.ColorToMove.Opposite());
        var moves = BitboardLookups.KingMoves[position] & ~attacked;

        var color = _board.ColorToMove;
        {
            var captures = moves & _board.GetColorBitboard(color.Opposite());
            Span<int> squares = stackalloc int[8];
            var count = captures.BitScanForwardAll(squares);
            destinationStart = WriteMoves(destination, destinationStart, position, squares, count, MoveType.Capture);
        }

        if (capturesOnly)
        {
            return destinationStart;
        }

        var occupancy = _board.OccupationBitboard;

        {
            var quiets = moves & ~occupancy;
            Span<int> squares = stackalloc int[8];
            var count = quiets.BitScanForwardAll(squares);
            destinationStart = WriteMoves(destination, destinationStart, position, squares, count, MoveType.Quiet);
        }

        if (_board.CheckersBitboard.PopCount() > 0)
        {
            return destinationStart;
        }

        var colorIndex = (int)color;
        if (_board.CastlingRights.CanCastle(color, CastleType.Kingside) &&
            (CastleOccupationKingside[colorIndex] & occupancy) == 0 &&
            (CastleKingsideShouldNotBeAttacked[colorIndex] & attacked) == 0)
        {
            destination[destinationStart++] = new Move
            {
                Start = position,
                End = Board.GetKingsideCastleKingEnd(color),
                Type = MoveType.KingsideCastle
            };
        }

        if (_board.CastlingRights.CanCastle(color, CastleType.Queenside) &&
            (CastleOccupationQueenside[colorIndex] & occupancy) == 0 &&
            (CastleQueensideShouldNotBeAttacked[colorIndex] & attacked) == 0)
        {
            destination[destinationStart++] = new Move
            {
                Start = position,
                End = Board.GetQueensideCastleKingEnd(color),
                Type = MoveType.QueensideCastle
            };
        }

        return destinationStart;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GenerateBishopMoves(Span<Move> destination, int destinationStart, int square,
        Bitboard pushMask, Bitboard captureMask, bool capturesOnly = false)
    {
        var color = _board.ColorToMove;
        var pins = _board.PinsInfo.Get(color);

        if (pins.OrthogonalPins.TestAt(square))
        {
            return destinationStart;
        }

        if (pins.DiagonalPins.TestAt(square))
        {
            pushMask &= pins.DiagonalPinnedMoves;
            captureMask &= pins.DiagonalPinnedMoves;
        }

        var occupancy = _board.OccupationBitboard;

        var diagonalOccupancy = occupancy & BitboardLookups.Diagonals[square];
        var antiDiagonalOccupancy = occupancy & BitboardLookups.AntiDiagonals[square];
        var moves =
            BitboardLookups.DiagonalMovesByOccupancy[square][diagonalOccupancy] |
            BitboardLookups.AntiDiagonalMovesByOccupancy[square][antiDiagonalOccupancy];

        {
            var captures = moves & _board.GetColorBitboard(color.Opposite()) & captureMask;
            Span<int> squares = stackalloc int[4];
            var count = captures.BitScanForwardAll(squares);
            destinationStart = WriteMoves(destination, destinationStart, square, squares, count, MoveType.Capture);
        }

        if (capturesOnly)
        {
            return destinationStart;
        }

        {
            var quiets = moves & ~occupancy & pushMask;
            Span<int> squares = stackalloc int[14];
            var count = quiets.BitScanForwardAll(squares);
            return WriteMoves(destination, destinationStart, square, squares, count, MoveType.Quiet);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GenerateQueenMoves(Span<Move> destination, int destinationStart, int position,
        Bitboard pushMask, Bitboard captureMask, bool capturesOnly = false)
    {
        destinationStart = GenerateBishopMoves(destination, destinationStart, position, pushMask, captureMask,
            capturesOnly);
        return GenerateRookMoves(destination, destinationStart, position, pushMask, captureMask, capturesOnly);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GenerateRookMoves(Span<Move> destination, int destinationStart, int square,
        Bitboard pushMask, Bitboard captureMask, bool capturesOnly = false)
    {
        var color = _board.ColorToMove;
        var pins = _board.PinsInfo.Get(color);

        if (pins.DiagonalPins.TestAt(square))
        {
            return destinationStart;
        }

        if (pins.OrthogonalPins.TestAt(square))
        {
            pushMask &= pins.OrthogonalPinnedMoves;
            captureMask &= pins.OrthogonalPinnedMoves;
        }

        var occupancy = _board.OccupationBitboard;
        var moves = BitboardLookups.GetFileMoves(square, occupancy) | BitboardLookups.GetRankMoves(square, occupancy);

        {
            var captures = moves & _board.GetColorBitboard(color.Opposite()) & captureMask;
            Span<int> squares = stackalloc int[4];
            var count = captures.BitScanForwardAll(squares);
            destinationStart = WriteMoves(destination, destinationStart, square, squares, count, MoveType.Capture);
        }

        if (capturesOnly)
        {
            return destinationStart;
        }

        {
            var quiets = moves & ~occupancy & pushMask;
            Span<int> squares = stackalloc int[14];
            var count = quiets.BitScanForwardAll(squares);
            return WriteMoves(destination, destinationStart, square, squares, count, MoveType.Quiet);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WriteMoves(Span<Move> destination, int destinationStart,
        int position, Span<int> targets, int targetsCount, MoveType moveType)
    {
        Debug.Assert(destination.Length > destinationStart + targetsCount);
        Debug.Assert(targets.Length >= targetsCount);

        for (var i = 0; i < targetsCount; i++)
        {
            destination[destinationStart++] = new Move
            {
                Start = position,
                End = targets[i],
                Type = moveType
            };
        }

        return destinationStart;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GenerateKnightMoves(Span<Move> destination, int destinationStart, int square,
        Bitboard pushMask, Bitboard captureMask,
        bool capturesOnly = false)
    {
        var color = _board.ColorToMove;
        var pins = _board.PinsInfo.Get(color);

        if ((pins.DiagonalPins | pins.OrthogonalPins).TestAt(square))
        {
            return destinationStart;
        }

        var moves = BitboardLookups.KnightMoves[square];
        var enemies = _board.GetColorBitboard(color.Opposite());

        {
            var captures = moves & enemies & captureMask;
            Span<int> squares = stackalloc int[8];
            var count = captures.BitScanForwardAll(squares);
            destinationStart = WriteMoves(destination, destinationStart, square, squares, count, MoveType.Capture);
        }

        if (capturesOnly)
        {
            return destinationStart;
        }

        {
            var quiets = moves & _board.FreeBitboard & pushMask;
            Span<int> squares = stackalloc int[8];
            var count = quiets.BitScanForwardAll(squares);
            return WriteMoves(destination, destinationStart, square, squares, count, MoveType.Quiet);
        }
    }

    private static readonly MoveType[] QuietPromotions =
    {
        MoveType.QueenPromotionQuiet, MoveType.BishopPromotionQuiet,
        MoveType.RookPromotionQuiet, MoveType.KnightPromotionQuiet
    };

    private static readonly MoveType[] CapturePromotions =
    {
        MoveType.QueenPromotionCapture, MoveType.BishopPromotionCapture,
        MoveType.RookPromotionCapture, MoveType.KnightPromotionCapture
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GeneratePawnMoves(Span<Move> destination, int destinationStart, int square,
        Bitboard optimizedPushMask, Bitboard captureMask,
        bool capturesOnly = false)
    {
        var color = _board.ColorToMove;
        var pins = _board.PinsInfo.Get(color);

        if (pins.DiagonalPins.TestAt(square))
        {
            optimizedPushMask = Bitboard.Empty;
            captureMask &= pins.DiagonalPinnedMoves;
        }
        else if (pins.OrthogonalPins.TestAt(square))
        {
            optimizedPushMask &= pins.OrthogonalPinnedMoves;
            captureMask = Bitboard.Empty;
        }

        var offset = color == PieceColor.Black ? 8 : -8;
        var attacks = BitboardLookups.PawnAttacks[(int)color][square];
        var enemies = _board.GetColorBitboard(color.Opposite());

        {
            var captures = attacks & enemies & captureMask;
            Span<int> squares = stackalloc int[2];
            var count = captures.BitScanForwardAll(squares);

            if ((square + offset) / 8 is 0 or 7)
            {
                for (var i = 0; i < count; i++)
                {
                    var promotions = enemies.TestAt(squares[i]) ? CapturePromotions : QuietPromotions;
                    foreach (var promotion in promotions)
                    {
                        destination[destinationStart++] = new Move
                        {
                            Start = square,
                            End = squares[i],
                            Type = promotion
                        };
                    }
                }
            }
            else
            {
                destinationStart = WriteMoves(destination, destinationStart, square, squares, count, MoveType.Capture);
            }
        }

        {
            var target = _board.GetEnPassantPosition();
            if (target != -1 && (attacks & captureMask).TestAt(target) && IsLegalEnPassant(square, target))
            {
                destination[destinationStart++] = new Move
                {
                    Start = square,
                    End = target,
                    Type = MoveType.EnPassant
                };
            }
        }

        if (capturesOnly)
        {
            return destinationStart;
        }

        if (_board.GetPieceBitboard(PieceType.Pawn).TestAt(square + offset))
        {
            return destinationStart;
        }

        {
            Span<int> pushes = stackalloc int[2];
            var pushBitboard = optimizedPushMask & BitboardLookups.PawnPushes[(int)color][square];

            var count = pushBitboard.BitScanForwardAll(pushes);
            for (var i = 0; i < count; i++)
            {
                var target = pushes[i];
                if (Math.Abs(target - square) / 8 == 1)
                {
                    if (target / 8 is 0 or 7)
                    {
                        foreach (var promotion in QuietPromotions)
                        {
                            destination[destinationStart++] = new Move
                            {
                                Start = square,
                                End = target,
                                Type = promotion
                            };
                        }
                    }
                    else
                    {
                        destination[destinationStart++] = new Move
                        {
                            Start = square,
                            End = target,
                            Type = MoveType.Quiet
                        };
                    }
                }
                else
                {
                    destination[destinationStart++] = new Move
                    {
                        Start = square,
                        End = target,
                        Type = MoveType.DoublePawn
                    };
                }
            }
        }

        return destinationStart;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsLegalEnPassant(int startSquare, int targetSquare)
    {
        var color = _board.GetColorAt(startSquare);
        var pawnsRank = BitboardLookups.Ranks[startSquare / 8];
        var king = _board.GetKingSquare(color);

        if (!pawnsRank.TestAt(king))
        {
            return true;
        }

        var orthSlidersBitboard = _board.GetPieceBitboard(PieceType.Rook) | _board.GetPieceBitboard(PieceType.Queen);
        var enemyOrthSlidersOnRank = orthSlidersBitboard & _board.GetColorBitboard(color.Opposite()) & pawnsRank;

        if (enemyOrthSlidersOnRank == 0)
        {
            return true;
        }

        var captureSquare = targetSquare + (color == PieceColor.Black ? -8 : 8);
        var rankOccupancyWithoutPawns = _board.OccupationBitboard & pawnsRank;
        rankOccupancyWithoutPawns.ResetAt(startSquare);
        rankOccupancyWithoutPawns.ResetAt(captureSquare);

        // 2 пешки и 1 король в ряду -> не более 5 фигур
        Span<int> orthSliders = stackalloc int[5];
        var count = enemyOrthSlidersOnRank.BitScanForwardAll(orthSliders);

        var sliderAttacks = Bitboard.Empty;
        for (var i = 0; i < count; i++)
        {
            var slider = orthSliders[i];
            sliderAttacks |= BitboardLookups.GetRankMoves(slider, rankOccupancyWithoutPawns);
        }

        return !sliderAttacks.TestAt(king);
    }

    public IEnumerable<Move> GetMovesFrom(int position)
    {
        var moves = GetLegalMoves().ToArray();
        return moves.Where(move => move.Start == position);
    }

    public static Dictionary<string, ulong> Perft(string fen, int depth)
    {
        var data = new Dictionary<string, ulong>();
        data["Nodes"] = 0;

        var board = new Board(fen);
        var moveGenerator = board.MoveGenerator;
        moveGenerator.Perft(data, depth);
        return data;
    }

    private void Perft(IDictionary<string, ulong> destination, int depth)
    {
        void IncrementKey(string key)
        {
            if (!destination.ContainsKey(key))
            {
                destination[key] = 0;
            }

            destination[key] += 1;
        }

        void WriteMoveTypes(ReadOnlySpan<Move> innerMoves)
        {
            foreach (var move in innerMoves)
            {
                var type = move.Type;
                IncrementKey("Nodes");
                switch (type)
                {
                    case MoveType.EnPassant:
                        IncrementKey("EnPassants");
                        goto case MoveType.Capture;
                    case MoveType.Capture:
                        IncrementKey("Captures");
                        break;
                    case MoveType.KingsideCastle:
                    case MoveType.QueensideCastle:
                        IncrementKey("Castles");
                        break;
                    case MoveType.BishopPromotionQuiet:
                    case MoveType.KnightPromotionQuiet:
                    case MoveType.RookPromotionQuiet:
                    case MoveType.QueenPromotionQuiet:
                        IncrementKey("Promotions");
                        break;
                    case MoveType.BishopPromotionCapture:
                    case MoveType.KnightPromotionCapture:
                    case MoveType.RookPromotionCapture:
                    case MoveType.QueenPromotionCapture:
                        IncrementKey("Promotions");
                        goto case MoveType.Capture;
                    case MoveType.Quiet:
                    case MoveType.DoublePawn:
                        break;
                    default:
                        Debug.Assert(false);
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        var moves = GetLegalMoves();

        if (depth == 1)
        {
            WriteMoveTypes(moves);
            return;
        }

        foreach (var move in moves)
        {
            _board.MakeMove(move);
            Perft(destination, depth - 1);
            _board.RevertMove();
        }
    }

    public static Dictionary<string, ulong> Divide(string fen, int depth)
    {
        var board = new Board(fen);
        var moveGenerator = board.MoveGenerator;

        var trace = new Dictionary<string, ulong>();
        moveGenerator.Divide(trace, depth);
        return trace;
    }

    private ulong Divide(Dictionary<string, ulong> destination, int depth)
    {
        var moves = GetLegalMoves();

        var filler = destination.Count == 0;
        if (filler)
        {
            foreach (var move in moves)
            {
                destination[move.ToString()] = depth == 1 ? 1UL : 0UL;
            }
        }

        if (depth == 1)
        {
            return (ulong)moves.Length;
        }

        var nodes = 0UL;
        foreach (var move in moves)
        {
            _board.MakeMove(move);
            var temp = Divide(destination, depth - 1);
            if (filler)
            {
                destination[move.ToString()] = temp;
            }

            nodes += temp;
            _board.RevertMove();
        }

        return nodes;
    }
}