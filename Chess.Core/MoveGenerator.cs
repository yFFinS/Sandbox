namespace Chess.Core;

public enum MoveType
{
    Quiet,
    Capture,
    DoublePawn,
    EnPassant,
    KingsideCastle,
    QueensideCastle,
    KnightPromotion,
    BishopPromotion,
    RookPromotion,
    QueenPromotion
}

public readonly struct Move
{
    public int Start { get; init; }
    public int End { get; init; }
    public MoveType Type { get; init; }

    public IEnumerable<int> GetPath()
    {
        yield return Start;
        yield return End;
    }

    public bool IsEmpty => Start == -1 || End == -1;

    public static readonly Move Empty = new()
    {
        Start = -1,
        End = -1
    };
}

public class MoveGenerator
{
    private static bool InBounds(int position)
    {
        return position is >= 0 and < 64;
    }

    private readonly ChessBoard _board;
    private Move[] _moves = null!;
    private int _generatedTurn = -1;

    public MoveGenerator(ChessBoard board)
    {
        _board = board;
    }

    public IReadOnlyList<Move> GetAllMoves()
    {
        if (_board.FullMoves == _generatedTurn)
        {
            return _moves;
        }

        if (_board.PinsDirty)
        {
            _board.RecalculatePinsAndCheckers();
        }

        _pins = _board.Pins.ToDictionary(pin => pin.Defender, pin => pin);
        var moves = GeneratePseudoLegalMoves();
        _moves = FilterLegalMoves(moves).ToArray();

        _generatedTurn = _board.FullMoves;

        return _moves;
    }

    private IEnumerable<Move> FilterLegalMoves(IEnumerable<Move> moves)
    {
        foreach (var move in moves)
        {
            var capturedPiece = _board.MakeMoveForLegalityCheck(move);
            var king = _board.GetKingPosition(_board.ColorToMove);
            if (!IsAttacked(king, _board.ColorToMove.Other()))
            {
                yield return move;
            }

            _board.RevertMoveForLegalityCheck(move, capturedPiece);
        }
    }

    private bool IsAttacked(int position, PieceColor attackedColor)
    {
        bool IsAttackedByBishop(int attackedPosition, Bitboard occupiedBb)
        {
            foreach (var bishop in _board.Bishops)
            {
                if (!_board.IsOfColorAt(attackedColor, bishop) ||
                    !(BitboardLookups.BishopAttacks[bishop].TestAt(attackedPosition)))
                {
                    continue;
                }

                var between = BitboardLookups.InBetween[bishop][attackedPosition];
                if ((between & occupiedBb) == between)
                {
                    return true;
                }
            }

            return false;
        }

        bool IsAttackedByRook(int attackedPosition, Bitboard occupiedBb)
        {
            foreach (var rook in _board.Rooks)
            {
                if (!_board.IsOfColorAt(attackedColor, rook) ||
                    !(BitboardLookups.RookAttacks[rook].TestAt(attackedPosition)))
                {
                    continue;
                }

                var between = BitboardLookups.InBetween[rook][attackedPosition];
                if ((between & occupiedBb) == between)
                {
                    return true;
                }
            }

            return false;
        }

        bool IsAttackedByQueen(int attackedPosition, Bitboard occupiedBb)
        {
            foreach (var queen in _board.Queens)
            {
                if (!_board.IsOfColorAt(attackedColor, queen) ||
                    !(BitboardLookups.QueenAttacks[queen].TestAt(attackedPosition)))
                {
                    continue;
                }

                var between = BitboardLookups.InBetween[queen][attackedPosition];
                if ((between & occupiedBb) == between)
                {
                    return true;
                }
            }

            return false;
        }

        var colorBB = _board.GetColorBitmapRef(attackedColor);

        var pawns = _board.GetPieceBitmapRef(PieceType.Pawn) & colorBB;
        if ((BitboardLookups.PawnAttacks[(int) attackedColor][position] & pawns) != 0)
        {
            return true;
        }

        var knights = _board.GetPieceBitmapRef(PieceType.Knight) & colorBB;
        if ((BitboardLookups.KnightAttacks[position] & knights) != 0)
        {
            return true;
        }

        var kings = _board.GetPieceBitmapRef(PieceType.King) & colorBB;
        if ((BitboardLookups.KingAttacks[position] & kings) != 0)
        {
            return true;
        }

        var occupiedBB = colorBB | _board.GetColorBitmapRef(attackedColor.Other());

        return IsAttackedByBishop(position, occupiedBB) | IsAttackedByRook(position, occupiedBB) |
               IsAttackedByQueen(position, occupiedBB);
    }

    private Dictionary<int, Pin> _pins = null!;

    private IEnumerable<Move> GeneratePseudoLegalMoves()
    {
        if (_board.TreatingCheck.Count > 1)
        {
            var kingPosition = _board.GetKingPosition(_board.ColorToMove);
            foreach (var move in GenerateKingMoves(kingPosition, _board.ColorToMove))
            {
                yield return move;
            }

            yield break;
        }

        foreach (var piece in _board.GetAllPieces())
        {
            var color = _board.ColorToMove;
            if (!_board.IsOfColorAt(color, piece))
            {
                continue;
            }

            var pieceType = _board.GetPieceTypeAt(piece);
            var generator = pieceType switch
            {
                PieceType.Pawn => GeneratePawnMoves(piece, color),
                PieceType.Rook => GenerateSlidingPieceMoves(piece, color, BitboardLookups.RookOffsets,
                    PieceMoveDirections.RookDirections),
                PieceType.Knight => GenerateLeapMoves(piece, color, BitboardLookups.KnightOffsets),
                PieceType.Bishop => GenerateSlidingPieceMoves(piece, color, BitboardLookups.BishopOffsets,
                    PieceMoveDirections.BishopDirections),
                PieceType.Queen => GenerateSlidingPieceMoves(piece, color, BitboardLookups.QueenOffsets,
                    PieceMoveDirections.QueenDirections),
                PieceType.King => GenerateKingMoves(piece, color),
                _ => throw new ArgumentOutOfRangeException()
            };

            foreach (var move in generator)
            {
                yield return move;
            }
        }
    }

    private static readonly int[][] CastlePositionsToCheckKingside =
    {
        new[] {4, 5, 6, 7},
        new[] {60, 61, 62, 63}
    };

    private static readonly int[][] CastlePositionsToCheckQueenside =
    {
        new[] {0, 1, 2, 3, 4},
        new[] {56, 57, 58, 59, 60}
    };

    private IEnumerable<Move> GenerateKingMoves(int position, PieceColor color)
    {
        foreach (var move in GenerateLeapMoves(position, color, BitboardLookups.KingOffsets))
        {
            yield return move;
        }

        if (_board.TreatingCheck.Count > 0)
        {
            yield break;
        }

        var canCastleKingside = color == PieceColor.Black
            ? _board.BlackCanCastleKingside
            : _board.WhiteCanCastleKingside;
        var canCastleQueenside = color == PieceColor.Black
            ? _board.BlackCanCastleQueenside
            : _board.WhiteCanCastleQueenside;

        var colorIndex = (int) color;
        if (canCastleKingside && CastlePositionsToCheckKingside[colorIndex].All(pos => !IsAttacked(pos, color.Other())))
        {
            yield return new Move
            {
                Start = position,
                End = color == PieceColor.Black ? 6 : 62,
                Type = MoveType.KingsideCastle
            };
        }

        if (canCastleQueenside &&
            CastlePositionsToCheckQueenside[colorIndex].All(pos => !IsAttacked(pos, color.Other())))
        {
            yield return new Move
            {
                Start = position,
                End = color == PieceColor.Black ? 1 : 57,
                Type = MoveType.QueensideCastle
            };
        }
    }

    private IEnumerable<Move> GenerateSlidingPieceMoves(int position, PieceColor color, int[][][] offsets, int[] dirs)
    {
        var pinned = _pins.TryGetValue(position, out var pin);

        var srv = offsets[position];
        for (var i = 0; i < dirs.Length; i++)
        {
            var dir = dirs[i];
            if (pinned && Math.Abs(pin.AttackerRay) != Math.Abs(dir))
            {
                continue;
            }

            foreach (var dist in srv[i])
            {
                var endPos = position + dir * dist;
                var occupied = _board.IsOccupied(endPos);
                if (occupied && _board.IsOfColorAt(color, endPos))
                {
                    continue;
                }

                yield return new Move
                {
                    Start = position,
                    End = endPos,
                    Type = occupied ? MoveType.Capture : MoveType.Quiet
                };

                if (occupied)
                {
                    break;
                }
            }
        }
    }

    private IEnumerable<Move> GenerateLeapMoves(int position, PieceColor color, int[][] offsets)
    {
        var pinned = _pins.TryGetValue(position, out var pin);
        foreach (var offset in offsets[position])
        {
            if (pinned && Math.Abs(pin.AttackerRay) != Math.Abs(offset))
            {
                continue;
            }

            var endPos = position + offset;
            var occupied = _board.IsOccupied(endPos);
            if (!occupied || !_board.IsOfColorAt(color, endPos))
            {
                yield return new Move
                {
                    Start = position,
                    End = endPos,
                    Type = occupied ? MoveType.Capture : MoveType.Quiet
                };
            }
        }
    }

    private static readonly MoveType[] Promotions =
        {MoveType.BishopPromotion, MoveType.KnightPromotion, MoveType.RookPromotion, MoveType.QueenPromotion};

    private IEnumerable<Move> GeneratePawnMoves(int position, PieceColor color)
    {
        var forwardFree = false;
        var offsets = BitboardLookups.PawnOffsets[(int) color][position];

        var pinned = _pins.TryGetValue(position, out var pin);
        foreach (var offset in offsets)
        {
            if (pinned && Math.Abs(pin.AttackerRay) != Math.Abs(offset))
            {
                continue;
            }

            if (!forwardFree && Math.Abs(offset) == 16)
            {
                continue;
            }

            var endPos = position + offset;

            var occupied = _board.IsOccupied(endPos);
            if (Math.Abs(offset) == 8 && occupied)
            {
                continue;
            }

            if (Math.Abs(offset) is 7 or 9)
            {
                if (occupied)
                {
                    if (_board.IsOfColorAt(color, endPos))
                    {
                        continue;
                    }
                }
                else if (_board.EnPassantPosition == endPos)
                {
                    yield return new Move
                    {
                        Start = position,
                        End = endPos,
                        Type = MoveType.EnPassant
                    };
                    continue;
                }
            }

            MoveType moveType;
            if (Math.Abs(offset) == 16)
            {
                moveType = MoveType.DoublePawn;
            }
            else if (Math.Abs(offset) == 8)
            {
                forwardFree = true;
                moveType = MoveType.Quiet;
            }
            else
            {
                moveType = MoveType.Capture;
            }

            if (endPos / 8 != 0 && endPos / 8 != 7)
            {
                yield return new Move
                {
                    Start = position,
                    End = endPos,
                    Type = moveType
                };
                yield break;
            }

            foreach (var promotion in Promotions)
            {
                yield return new Move
                {
                    Start = position,
                    End = endPos,
                    Type = promotion
                };
            }
        }
    }

    public IEnumerable<Move> GetMovesFrom(int position)
    {
        return GetAllMoves().Where(move => move.Start == position);
    }

    public static ulong Perft(string fen, int depth)
    {
        var board = new ChessBoard(fen);
        var moveGenerator = board.MoveGenerator;
        return moveGenerator.Perft(depth);
    }

    private ulong Perft(int depth)
    {
        var moves = GetAllMoves();

        if (depth == 1)
        {
            return (ulong) moves.Count;
        }

        var nodes = 0UL;
        foreach (var move in moves)
        {
            _board.MakeMove(move);
            nodes += Perft(depth - 1);
            _board.RevertMove();
        }

        return nodes;
    }
}

public static class PieceColorExtensions
{
    public static PieceColor Other(this PieceColor color)
    {
        return color == PieceColor.Black ? PieceColor.White : PieceColor.Black;
    }
}