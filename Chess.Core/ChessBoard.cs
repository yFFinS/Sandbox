using System.Diagnostics;

namespace Chess.Core;

internal static class PieceMoveDirections
{
    public static readonly int[] BishopDirections = {-9, -7, 7, 9};
    public static readonly int[] RookDirections = {-1, 1, -8, 8};
    public static readonly int[] QueenDirections = {-1, 1, -8, 8, -9, 9, -7, 7};
}

internal static class BitboardLookups
{
    public static readonly int[][][] PawnOffsets = new int[2][][];
    public static readonly int[][][] RookOffsets = new int[64][][];
    public static readonly int[][] KnightOffsets = new int[64][];
    public static readonly int[][][] BishopOffsets = new int[64][][];
    public static readonly int[][][] QueenOffsets = new int[64][][];
    public static readonly int[][] KingOffsets = new int[64][];

    public static readonly Bitboard[][] PawnAttacks = new Bitboard[2][];
    public static readonly Bitboard[] KnightAttacks = new Bitboard[64];
    public static readonly Bitboard[] KingAttacks = new Bitboard[64];
    public static readonly Bitboard[] BishopAttacks = new Bitboard[64];
    public static readonly Bitboard[] RookAttacks = new Bitboard[64];
    public static readonly Bitboard[] QueenAttacks = new Bitboard[64];

    public static readonly Bitboard[][] InBetween = new Bitboard[64][];

    private static bool InBounds(int position)
    {
        return position is >= 0 and < 64;
    }

    static BitboardLookups()
    {
        PreComputeInBetween();

        PreComputePawnMoves();
        PreComputeKnightMoves();
        PreComputeBishopMoves();
        PreComputeRookMoves();
        PreComputeQueenMoves();
        PreComputeKingMoves();
    }

    private static void PreComputeInBetween()
    {
        // https://www.chessprogramming.org/Square_Attacked_By

        const ulong m1 = ~0UL;
        const ulong a2a7 = 0x0001010101010100UL;
        const ulong b2g7 = 0x0040201008040200UL;
        const ulong h1b7 = 0x0002040810204080UL;

        for (var from = 0; from < 64; from++)
        {
            InBetween[from] = new Bitboard[64];
            for (var to = 0; to < 64; to++)
            {
                var between = (m1 << from) ^ (m1 << to);
                var file = (ulong) ((to & 7) - (from & 7));
                var rank = (ulong) ((to | 7) - from) >> 3;
                var line = ((file & 7) - 1) & a2a7;
                line += 2 * (((rank & 7) - 1) >> 58);
                line += (((rank - file) & 15) - 1) & b2g7;
                line += (((rank + file) & 15) - 1) & h1b7;
                // ? line *= btwn & -btwn;
                line *= between;
                InBetween[from][to] = line & between;
            }
        }
    }

    private static void PreComputeBishopMoves()
    {
        for (var pos = 0; pos < 64; pos++)
        {
            BishopOffsets[pos] = new int[4][];

            foreach (var (ind, offsetDir) in PieceMoveDirections.BishopDirections.Select((i, o) => (i, o)))
            {
                var validDists = new List<int>();
                for (var dist = 1; dist < 8; dist++)
                {
                    var offset = offsetDir * dist;
                    if (InBounds(pos + offset))
                    {
                        BishopAttacks[pos].SetAt(pos + offset);
                        validDists.Add(dist);
                    }
                }

                BishopOffsets[pos][ind] = validDists.ToArray();
            }
        }
    }

    private static void PreComputeRookMoves()
    {
        for (var pos = 0; pos < 64; pos++)
        {
            RookOffsets[pos] = new int[4][];

            foreach (var (ind, offsetDir) in PieceMoveDirections.RookDirections.Select((i, o) => (i, o)))
            {
                var validDists = new List<int>();
                for (var dist = 1; dist < 8; dist++)
                {
                    var offset = offsetDir * dist;
                    if (InBounds(pos + offset))
                    {
                        validDists.Add(dist);
                        RookAttacks[pos].SetAt(pos + offset);
                    }
                }

                RookOffsets[pos][ind] = validDists.ToArray();
            }
        }
    }

    private static void PreComputeQueenMoves()
    {
        for (var pos = 0; pos < 64; pos++)
        {
            QueenOffsets[pos] = new int[8][];
            foreach (var (ind, offsetDir) in PieceMoveDirections.QueenDirections.Select((i, o) => (i, o)))
            {
                var validDists = new List<int>();
                for (var dist = 1; dist < 8; dist++)
                {
                    var offset = offsetDir * dist;
                    if (InBounds(pos + offset))
                    {
                        validDists.Add(dist);
                        QueenAttacks[pos].SetAt(pos + offset);
                    }
                }

                QueenOffsets[pos][ind] = validDists.ToArray();
            }
        }
    }

    private static void PreComputeKingMoves()
    {
        for (var pos = 0; pos < 64; pos++)
        {
            var validOffsets = new List<int>();
            foreach (var offset in new[] {-9, -8, -7, -1, 1, 7, 8, 9})
            {
                if (InBounds(pos + offset))
                {
                    validOffsets.Add(offset);
                    KingAttacks[pos + offset].SetAt(pos);
                }
            }

            KingOffsets[pos] = validOffsets.ToArray();
        }
    }

    private static void PreComputeKnightMoves()
    {
        for (var pos = 0; pos < 64; pos++)
        {
            var validOffsets = new List<int>();

            foreach (var offset in new[] {-10, -17, -15, -6, 10, 17, 15, 6})
            {
                if (InBounds(pos + offset))
                {
                    validOffsets.Add(offset);
                    KnightAttacks[pos + offset].SetAt(pos);
                }
            }

            KnightOffsets[pos] = validOffsets.ToArray();
        }
    }

    private static void PreComputePawnMoves()
    {
        foreach (var color in new[] {PieceColor.Black, PieceColor.White})
        {
            var dest = PawnOffsets[(int) color] = new int[64][];
            var destAttacks = PawnAttacks[(int) color] = new Bitboard[64];

            var colorSign = color == PieceColor.Black ? 1 : -1;
            for (var pos = 0; pos < 64; pos++)
            {
                var validOffsets = new List<int>();

                foreach (var offset in new[] {7, 8, 9}.Select(o => o * colorSign))
                {
                    if (InBounds(pos + offset))
                    {
                        if (Math.Abs(offset) != 8)
                        {
                            destAttacks[pos + offset].SetAt(pos);
                        }

                        validOffsets.Add(offset);
                    }
                }

                if (pos / 8 == 1 && color == PieceColor.Black || pos / 8 == 6 && color == PieceColor.White)
                {
                    validOffsets.Add(2 * colorSign);
                }

                dest[pos] = validOffsets.ToArray();
            }
        }
    }
}

public struct Bitboard
{
    public Bitboard(ulong value)
    {
        Value = value;
    }

    public ulong Value { get; private set; }

    public readonly Bitboard GetAt(int offset)
    {
        return Value & (1UL << offset);
    }

    public static Bitboard FromSetBits(IEnumerable<int> bits)
    {
        return bits.Aggregate(0UL, (current, offset) => current | 1UL << offset);
    }

    public void SetAt(int offset)
    {
        Value |= (1UL << offset);
    }

    public void ResetAt(int offset)
    {
        Value &= ~(1UL << offset);
    }

    public void Reset()
    {
        Value = 0;
    }

    public readonly IEnumerable<int> GetAllSetBits()
    {
        for (var i = 0; i < 64; ++i)
        {
            if ((Value & (1UL << i)) != 0)
            {
                yield return i;
            }
        }
    }

    public static implicit operator Bitboard(ulong value)
    {
        return new Bitboard(value);
    }

    public static implicit operator ulong(Bitboard bitmap)
    {
        return bitmap.Value;
    }

    public bool TestAt(int offset)
    {
        return (Value & (1UL << offset)) != 0;
    }

    public static Bitboard operator |(Bitboard lhs, Bitboard rhs)
    {
        return lhs.Value | rhs.Value;
    }

    public static Bitboard operator &(Bitboard lhs, Bitboard rhs)
    {
        return lhs.Value & rhs.Value;
    }

    public static Bitboard operator ^(Bitboard lhs, Bitboard rhs)
    {
        return lhs.Value ^ rhs.Value;
    }

    public static Bitboard operator ~(Bitboard bitboard)
    {
        return ~bitboard.Value;
    }

    public static Bitboard operator >> (Bitboard bitboard, int offset)
    {
        return bitboard.Value >> offset;
    }

    public static Bitboard operator <<(Bitboard bitboard, int offset)
    {
        return bitboard.Value << offset;
    }
}

public class ChessBoard
{
    public const string StartFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    public PieceColor ColorToMove { get; private set; }

    private readonly Bitboard[] _colorBitboards = new Bitboard[2];
    private readonly Bitboard[] _pieceBitboards = new Bitboard[6];

    public bool WhiteCanCastleKingside { get; private set; }
    public bool WhiteCanCastleQueenside { get; private set; }
    public bool BlackCanCastleKingside { get; private set; }
    public bool BlackCanCastleQueenside { get; private set; }
    public int EnPassantPosition { get; private set; }
    public int HalfMoves { get; private set; }
    public int FullMoves { get; private set; }

    internal ref Bitboard GetColorBitmapRef(PieceColor color)
    {
        return ref _colorBitboards[(int) color];
    }

    internal ref Bitboard GetPieceBitmapRef(PieceType pieceType)
    {
        return ref _pieceBitboards[(int) pieceType];
    }

    public static string ParseCellName(int position)
    {
        return (char) ('a' + position % 8) + (8 - position / 8).ToString();
    }

    public bool IsOfColorAt(PieceColor color, int position)
    {
        return GetColorBitmapRef(color).TestAt(position);
    }

    public readonly MoveGenerator MoveGenerator;

    public ChessBoard()
    {
        MoveGenerator = new MoveGenerator(this);
    }

    public void SetPieceAt(int position, Piece piece)
    {
        ClearAt(position);

        if (piece.IsEmpty)
        {
            return;
        }

        GetColorBitmapRef(piece.Color).SetAt(position);
        GetPieceBitmapRef(piece.Type).SetAt(position);
        GetPieceListOfType(piece.Type).Add(position);

        PinsDirty = true;
    }

    internal bool PinsDirty { get; set; }

    private static readonly Dictionary<PieceType, int[]> PinningRays = new()
    {
        {PieceType.Bishop, new[] {-7, 7, -9, 9}},
        {PieceType.Rook, new[] {-1, 1, -8, 8}},
        {PieceType.Queen, new[] {-1, 1, -8, 8, -7, 7, -9, 9}}
    };

    private static readonly Dictionary<Piece, int[]> CheckOffsets = new()
    {
        {new Piece(PieceColor.Black, PieceType.Pawn), new[] {7, 9}},
        {new Piece(PieceColor.White, PieceType.Pawn), new[] {-9, -7}},
        {new Piece(PieceColor.Black, PieceType.Knight), new[] {-10, -17, -15, -6, 10, 17, 15, 6}},
        {new Piece(PieceColor.White, PieceType.Knight), new[] {-10, -17, -15, -6, 10, 17, 15, 6}}
    };

    internal void RecalculatePinsAndCheckers(bool force = false)
    {
        if (!PinsDirty && !force)
        {
            return;
        }

        _pins.Clear();
        _treatingCheck.Clear();

        foreach (var pinner in _bishops.Concat(_rooks).Concat(_queens))
        {
            if (IsOfColorAt(ColorToMove, pinner))
            {
                continue;
            }

            var pinnerType = GetPieceTypeAt(pinner);
            foreach (var ray in PinningRays[pinnerType])
            {
                var defender = FindFirstPieceInDirection(pinner, ray);
                if (defender == -1 || !IsOfColorAt(ColorToMove, defender))
                {
                    continue;
                }

                if (_kings.Contains(defender))
                {
                    _treatingCheck.Add(pinner);
                }
                else
                {
                    var target = FindFirstPieceInDirection(defender, ray);
                    if (_kings.Contains(target))
                    {
                        _pins.Add(new Pin
                        {
                            Attacker = pinner,
                            Defender = defender,
                            AttackerRay = ray
                        });
                    }
                }
            }
        }

        foreach (var checker in _pawns.Concat(_knights))
        {
            var checkerPiece = GetPieceAt(checker);
            if (checkerPiece.Color == ColorToMove)
            {
                continue;
            }

            foreach (var offset in CheckOffsets[checkerPiece])
            {
                var target = checker + offset;
                if (target is < 0 or >= 64)
                {
                    continue;
                }

                if (IsOfTypeAt(PieceType.King, target) && IsOfColorAt(ColorToMove, target))
                {
                    _treatingCheck.Add(checker);
                }
            }
        }

        PinsDirty = false;
    }

    public bool IsOfTypeAt(PieceType pieceType, int position)
    {
        return GetPieceBitmapRef(pieceType).TestAt(position);
    }

    internal int FindFirstPieceInDirection(int start, int direction)
    {
        while (true)
        {
            start += direction;

            if (start is < 0 or >= 64)
            {
                return -1;
            }

            if (IsOccupied(start))
            {
                return start;
            }
        }
    }

    internal void ClearAt(int position)
    {
        for (var i = 0; i < _colorBitboards.Length; i++)
        {
            _colorBitboards[i].ResetAt(position);
        }

        for (var i = 0; i < _pieceBitboards.Length; i++)
        {
            if (!_pieceBitboards[i].TestAt(i))
            {
                continue;
            }

            var pieceType = (PieceType) i;
            GetPieceListOfType(pieceType).Remove(i);
            _pieceBitboards[i].ResetAt(position);
            break;
        }

        PinsDirty = true;
    }

    private static readonly Dictionary<char, PieceType> PieceNotationMap = new()
    {
        {'p', PieceType.Pawn}, {'n', PieceType.Knight},
        {'b', PieceType.Bishop}, {'r', PieceType.Rook},
        {'q', PieceType.Queen}, {'k', PieceType.King}
    };

    public static int ParsePosition(string cellName)
    {
        return (8 - cellName[1]) * 8 + (cellName[0] - 'a');
    }

    public void ParseFEN(string text)
    {
        Clear();

        var split = text.Split();

        ColorToMove = split[1] == "w" ? PieceColor.White : PieceColor.Black;

        BlackCanCastleKingside = split[2].Contains('k');
        BlackCanCastleQueenside = split[2].Contains('q');
        WhiteCanCastleKingside = split[2].Contains('K');
        WhiteCanCastleQueenside = split[2].Contains('Q');

        EnPassantPosition = split[3] == "-" ? -1 : ParsePosition(split[3]);
        HalfMoves = int.Parse(split[4]);
        FullMoves = int.Parse(split[5]);

        var index = 0;
        foreach (var ch in split[0])
        {
            if (char.IsDigit(ch))
            {
                index += ch - '0';
            }
            else if (PieceNotationMap.TryGetValue(char.ToLower(ch), out var pieceType))
            {
                var color = char.IsUpper(ch) ? PieceColor.White : PieceColor.Black;
                SetPieceAt(index++, new Piece(color, pieceType));
            }
        }
    }

    public void ResetToDefaultPosition()
    {
        ParseFEN(StartFEN);
    }

    public void Clear()
    {
        for (var i = 0; i < _colorBitboards.Length; i++)
        {
            _colorBitboards[i].Reset();
        }

        for (var i = 0; i < _pieceBitboards.Length; i++)
        {
            _pieceBitboards[i].Reset();
        }

        _pawns.Clear();
        _bishops.Clear();
        _knights.Clear();
        _queens.Clear();
        _rooks.Clear();
        _kings.Clear();
        _pins.Clear();
        _treatingCheck.Clear();

        HalfMoves = 0;
        FullMoves = 0;
        ColorToMove = PieceColor.Black;
        WhiteCanCastleKingside = false;
        WhiteCanCastleQueenside = false;
        BlackCanCastleKingside = false;
        BlackCanCastleQueenside = false;
        EnPassantPosition = -1;
    }

    public IEnumerable<int> GetAllPieces()
    {
        return _pawns.Concat(_bishops).Concat(_knights).Concat(_rooks).Concat(_queens).Concat(_kings);
    }

    internal List<int> GetPieceListOfType(PieceType pieceType)
    {
        return pieceType switch
        {
            PieceType.Pawn => _pawns,
            PieceType.Knight => _knights,
            PieceType.Bishop => _bishops,
            PieceType.Rook => _rooks,
            PieceType.Queen => _queens,
            PieceType.King => _kings,
            _ => throw new ArgumentOutOfRangeException(nameof(pieceType), pieceType, null)
        };
    }

    public IEnumerable<int> GetAllPiecesOfType(PieceType pieceType)
    {
        return GetPieceListOfType(pieceType);
    }

    public Piece GetPieceAt(int position)
    {
        return IsEmpty(position)
            ? Piece.Empty
            : new Piece(GetColorAt(position), GetPieceTypeAt(position));
    }

    public Piece GetPieceOfTypeAt(PieceType type, int position)
    {
        return IsEmpty(position)
            ? Piece.Empty
            : new Piece(GetColorAt(position), type);
    }

    internal PieceColor GetColorAt(int position)
    {
        return GetColorBitmapRef(PieceColor.Black).TestAt(position) ? PieceColor.Black : PieceColor.White;
    }

    internal PieceType GetPieceTypeAt(int position)
    {
        for (var i = 0; i < _pieceBitboards.Length - 1; i++)
        {
            if (IsOccupied(i))
            {
                return (PieceType) i;
            }
        }

        return (PieceType) (_pieceBitboards.Length - 1);
    }

    public bool IsEmpty(int position)
    {
        return !IsOccupied(position);
    }

    public bool IsOccupied(int position)
    {
        return (_colorBitboards[0] | _colorBitboards[1]).TestAt(position);
    }

    public GameEndState GetGameEndState()
    {
        var isCheckmate = IsCheckmate();
        var canMove = MoveGenerator.GetAllMoves().Count > 0;
        if (!canMove && !isCheckmate)
        {
            return GameEndState.Draw;
        }

        if (isCheckmate)
        {
            return ColorToMove == PieceColor.White ? GameEndState.BlackWin : GameEndState.WhiteWin;
        }

        return GameEndState.None;
    }

    public bool IsGameEnded()
    {
        return GetGameEndState() != GameEndState.None;
    }

    public bool IsCheck()
    {
        return _treatingCheck.Count > 0;
    }

    public bool IsCheckmate()
    {
        return IsCheck() && MoveGenerator.GetAllMoves().Count == 0;
    }

    public int GetKingPosition(PieceColor color)
    {
        return Kings.First(pos => IsOfColorAt(color, pos));
    }

    public void MakeMove(Move move)
    {
        ColorToMove = ColorToMove.Other();
        var undoHalfMoves = HalfMoves++;

        var capturedPiece = Piece.Empty;

        var blackOo = BlackCanCastleKingside;
        var blackOoo = BlackCanCastleQueenside;
        var whiteOo = WhiteCanCastleKingside;
        var whiteOoo = WhiteCanCastleQueenside;
        var enPassantPosition = EnPassantPosition;

        var movingPiece = GetPieceAt(move.Start);
        if (movingPiece.Type == PieceType.Pawn)
        {
            HalfMoves = 0;
        }

        ClearAt(move.Start);

        switch (move.Type)
        {
            case MoveType.Quiet:
                break;
            case MoveType.Capture:
                capturedPiece = GetPieceAt(move.End);
                if (!capturedPiece.IsEmpty)
                {
                    if (GetKingsideCastleRookStart(capturedPiece.Color) == move.End)
                    {
                        if (capturedPiece.Color == PieceColor.Black)
                        {
                            BlackCanCastleKingside = false;
                        }
                        else
                        {
                            WhiteCanCastleKingside = false;
                        }
                    }

                    if (GetQueensideCastleRookEnd(capturedPiece.Color) == move.End)
                    {
                        if (capturedPiece.Color == PieceColor.Black)
                        {
                            BlackCanCastleQueenside = false;
                        }
                        else
                        {
                            WhiteCanCastleQueenside = false;
                        }
                    }
                }

                break;
            case MoveType.DoublePawn:
                EnPassantPosition = move.Start + (move.End - move.Start) / 8;
                break;
            case MoveType.EnPassant:
                var capturedPosition = move.Start + (move.End - EnPassantPosition) / 8;
                capturedPiece = GetPieceAt(capturedPosition);
                break;
            case MoveType.KingsideCastle:
                ClearAt(movingPiece.Color == PieceColor.Black ? 7 : 63);
                SetPieceAt(movingPiece.Color == PieceColor.Black ? 5 : 61,
                    new Piece(movingPiece.Color, PieceType.Rook));
                if (movingPiece.Color == PieceColor.Black)
                {
                    BlackCanCastleKingside = false;
                }
                else
                {
                    WhiteCanCastleKingside = false;
                }

                break;
            case MoveType.QueensideCastle:
                ClearAt(movingPiece.Color == PieceColor.Black ? 0 : 56);
                SetPieceAt(movingPiece.Color == PieceColor.Black ? 2 : 58,
                    new Piece(movingPiece.Color, PieceType.Rook));
                if (movingPiece.Color == PieceColor.Black)
                {
                    BlackCanCastleQueenside = false;
                }
                else
                {
                    WhiteCanCastleQueenside = false;
                }

                break;
            case MoveType.KnightPromotion:
                movingPiece = new Piece(movingPiece.Color, PieceType.Knight);
                goto case MoveType.Capture;
            case MoveType.BishopPromotion:
                movingPiece = new Piece(movingPiece.Color, PieceType.Bishop);
                goto case MoveType.Capture;
            case MoveType.RookPromotion:
                movingPiece = new Piece(movingPiece.Color, PieceType.Rook);
                goto case MoveType.Capture;
            case MoveType.QueenPromotion:
                movingPiece = new Piece(movingPiece.Color, PieceType.Queen);
                goto case MoveType.Capture;
            default:
                throw new ArgumentOutOfRangeException();
        }

        SetPieceAt(move.End, movingPiece);

        if (movingPiece.Color == PieceColor.Black)
        {
            FullMoves++;
        }

        if (!capturedPiece.IsEmpty)
        {
            HalfMoves = 0;
        }

        _undoInfos.Push(new MoveUndoInfo
        {
            Move = move,
            CapturedPiece = capturedPiece,
            EnPassantPosition = enPassantPosition,
            BlackCanCastleKingside = blackOo,
            BlackCanCastleQueenside = blackOoo,
            WhiteCanCastleKingside = whiteOo,
            WhiteCanCastleQueenside = whiteOoo,
            HalfMoves = undoHalfMoves,
            Pins = _pins.ToArray(),
            TreatingCheck = _treatingCheck.ToArray()
        });

        RecalculatePinsAndCheckers();
    }

    public void RevertMove()
    {
        var undoInfo = _undoInfos.Pop();

        EnPassantPosition = undoInfo.EnPassantPosition;
        BlackCanCastleKingside = undoInfo.BlackCanCastleKingside;
        BlackCanCastleQueenside = undoInfo.BlackCanCastleQueenside;
        WhiteCanCastleKingside = undoInfo.WhiteCanCastleKingside;
        WhiteCanCastleQueenside = undoInfo.WhiteCanCastleQueenside;
        HalfMoves = undoInfo.HalfMoves;
        FullMoves--;

        ColorToMove = ColorToMove.Other();

        var move = undoInfo.Move;

        var movedPiece = GetPieceAt(move.End);
        ClearAt(move.End);

        var blackMoved = movedPiece.Color == PieceColor.Black;

        switch (move.Type)
        {
            case MoveType.Quiet:
                break;
            case MoveType.Capture:
                SetPieceAt(move.End, undoInfo.CapturedPiece);
                break;
            case MoveType.DoublePawn:
                EnPassantPosition = move.Start + (move.End - move.Start) / 8;
                break;
            case MoveType.EnPassant:
                var capturedPosition = move.Start + (move.End - EnPassantPosition) / 8;
                SetPieceAt(capturedPosition, undoInfo.CapturedPiece);
                break;
            case MoveType.KingsideCastle:
                ClearAt(blackMoved ? 6 : 62);
                ClearAt(blackMoved ? 5 : 61);
                SetPieceAt(blackMoved ? 7 : 63, new Piece(movedPiece.Color, PieceType.Rook));
                SetPieceAt(blackMoved ? 4 : 60, movedPiece);
                break;
            case MoveType.QueensideCastle:
                ClearAt(blackMoved ? 1 : 57);
                ClearAt(blackMoved ? 2 : 58);
                SetPieceAt(blackMoved ? 0 : 56, new Piece(movedPiece.Color, PieceType.Rook));
                SetPieceAt(blackMoved ? 4 : 60, movedPiece);
                break;
            case MoveType.KnightPromotion:
            case MoveType.BishopPromotion:
            case MoveType.RookPromotion:
            case MoveType.QueenPromotion:
                movedPiece = new Piece(movedPiece.Color, PieceType.Pawn);
                goto case MoveType.Capture;
            default:
                throw new ArgumentOutOfRangeException();
        }

        SetPieceAt(move.Start, movedPiece);

        _pins.Clear();
        _treatingCheck.Clear();
        _pins.AddRange(undoInfo.Pins);
        _treatingCheck.AddRange(undoInfo.TreatingCheck);

        PinsDirty = false;
    }

    private readonly Stack<MoveUndoInfo> _undoInfos = new(256);

    private readonly List<int> _pawns = new(16);
    private readonly List<int> _rooks = new(4);
    private readonly List<int> _bishops = new(4);
    private readonly List<int> _knights = new(4);
    private readonly List<int> _queens = new(2);
    private readonly List<int> _kings = new(2);

    private readonly List<int> _treatingCheck = new(8);
    private readonly List<Pin> _pins = new(8);

    public ChessBoard(string fen) : this()
    {
        ParseFEN(fen);
    }

    public IReadOnlyList<Pin> Pins => _pins;
    public IReadOnlyList<int> TreatingCheck => _treatingCheck;

    public IReadOnlyList<int> Pawns => _pawns;
    public IReadOnlyList<int> Rooks => _rooks;
    public IReadOnlyList<int> Bishops => _bishops;
    public IReadOnlyList<int> Knights => _knights;
    public IReadOnlyList<int> Queens => _queens;
    public IReadOnlyList<int> Kings => _kings;

    internal Piece MakeMoveForLegalityCheck(Move move)
    {
        // Must be reverted before any other move

        var capturedPiece = Piece.Empty;
        var movedPiece = GetPieceAt(move.Start);
        ClearAt(move.Start);

        var blackMoved = movedPiece.Color == PieceColor.Black;

        switch (move.Type)
        {
            case MoveType.Quiet:
            case MoveType.DoublePawn:
                break;
            case MoveType.KnightPromotion:
            case MoveType.BishopPromotion:
            case MoveType.RookPromotion:
            case MoveType.QueenPromotion:
            case MoveType.Capture:
                capturedPiece = GetPieceAt(move.End);
                break;
            case MoveType.EnPassant:
                var capturedPosition = move.Start + (move.End - EnPassantPosition) / 8;
                capturedPiece = GetPieceAt(capturedPosition);
                ClearAt(capturedPosition);
                break;
            case MoveType.KingsideCastle:
                ClearAt(blackMoved ? 7 : 63);
                SetPieceAt(blackMoved ? 5 : 61, new Piece(movedPiece.Color, PieceType.Rook));
                break;
            case MoveType.QueensideCastle:
                ClearAt(blackMoved ? 0 : 56);
                SetPieceAt(blackMoved ? 2 : 58, new Piece(movedPiece.Color, PieceType.Rook));
                break;
            default:
                Debug.Assert(false);
                throw new ArgumentOutOfRangeException();
        }

        SetPieceAt(move.End, movedPiece);
        PinsDirty = false;

        return capturedPiece;
    }

    internal void RevertMoveForLegalityCheck(Move move, Piece capturedPiece)
    {
        var movedPiece = GetPieceAt(move.End);
        ClearAt(move.End);

        var blackMoved = movedPiece.Color == PieceColor.Black;

        switch (move.Type)
        {
            case MoveType.DoublePawn:
            case MoveType.Quiet:
                break;
            case MoveType.KnightPromotion:
            case MoveType.BishopPromotion:
            case MoveType.RookPromotion:
            case MoveType.QueenPromotion:
            case MoveType.Capture:
                SetPieceAt(move.End, capturedPiece);
                break;
            case MoveType.EnPassant:
                var capturedPosition = move.Start + (move.End - EnPassantPosition) / 8;
                SetPieceAt(capturedPosition, capturedPiece);
                break;
            case MoveType.KingsideCastle:
                ClearAt(blackMoved ? 6 : 62);
                ClearAt(blackMoved ? 5 : 61);
                SetPieceAt(blackMoved ? 7 : 63, new Piece(movedPiece.Color, PieceType.Rook));
                SetPieceAt(blackMoved ? 4 : 60, movedPiece);
                break;
            case MoveType.QueensideCastle:
                ClearAt(blackMoved ? 1 : 57);
                ClearAt(blackMoved ? 2 : 58);
                SetPieceAt(blackMoved ? 0 : 56, new Piece(movedPiece.Color, PieceType.Rook));
                SetPieceAt(blackMoved ? 4 : 60, movedPiece);
                break;
            default:
                Debug.Assert(false);
                throw new ArgumentOutOfRangeException();
        }

        SetPieceAt(move.Start, movedPiece);
        PinsDirty = false;
    }

    public int GetKingsideCastleRookStart(PieceColor color)
    {
        return color == PieceColor.Black ? 7 : 53;
    }

    public int GetQueensideCastleRookStart(PieceColor color)
    {
        return color == PieceColor.Black ? 0 : 56;
    }

    public int GetKingsideCastleRookEnd(PieceColor color)
    {
        return color == PieceColor.Black ? 5 : 61;
    }

    public int GetQueensideCastleRookEnd(PieceColor color)
    {
        return color == PieceColor.Black ? 2 : 58;
    }

    public int GetEnPassantCapturedPawn(Move move)
    {
        var color = GetColorAt(move.Start);
        var blackMoved = color == PieceColor.Black;

        switch (move.Type)
        {
            case MoveType.EnPassant:
                return blackMoved ? move.End - move.Start - 8 : move.End - move.Start + 8;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

public readonly struct Pin
{
    public int Defender { get; init; }
    public int Attacker { get; init; }
    public int AttackerRay { get; init; }
}

public readonly struct MoveUndoInfo
{
    public Move Move { get; init; }
    public Piece CapturedPiece { get; init; }

    public int EnPassantPosition { get; init; }
    public bool WhiteCanCastleKingside { get; init; }
    public bool WhiteCanCastleQueenside { get; init; }
    public bool BlackCanCastleKingside { get; init; }
    public bool BlackCanCastleQueenside { get; init; }
    public int HalfMoves { get; init; }

    public Pin[] Pins { get; init; }
    public int[] TreatingCheck { get; init; }
}