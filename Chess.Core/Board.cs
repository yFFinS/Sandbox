using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Chess.Core;

public class Board
{
    public const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    public PieceColor ColorToMove { get; private set; }

    private readonly Bitboard[] _colorBitboards = new Bitboard[2];
    private readonly Bitboard[] _pieceBitboards = new Bitboard[6];

    private ByColorIndexer<Bitboard> _attackedBitboardIgnoreKing;
    private ByColorIndexer<PinsInfo> _pinsInfo;
    private Bitboard _checkersBitboard;
    private Bitboard _occupationBitboard;
    public Bitboard CheckersBitboard => _checkersBitboard;
    public Bitboard OccupationBitboard => _occupationBitboard;

    private readonly PieceType[] _pieceTypes = new PieceType[64];
    private readonly PieceColor[] _pieceColors = new PieceColor[64];
    public ReadOnlyColorIndexer<Bitboard> AttackedBitboardIgnoreKing => new(_attackedBitboardIgnoreKing);
    public ReadOnlyColorIndexer<PinsInfo> PinsInfo => new(_pinsInfo);

    private CastlingRights _castlingRights;
    public CastlingRights CastlingRights => _castlingRights;

    public readonly PieceSquareEvaluator PieceSquareEvaluator = new();
    public int EnPassantFile { get; private set; }
    public int HalfMoves { get; private set; }
    public int FullMoves { get; private set; }

    public bool IsEndGame { get; private set; }

    public string ToFen()
    {
        var fen = new StringBuilder();

        var skipLength = 0;
        for (var pos = 0; pos < 64; pos++)
        {
            var piece = GetPieceAt(pos);
            if (piece.IsEmpty)
            {
                skipLength++;
            }
            else
            {
                var letter = piece.Type switch
                {
                    PieceType.Pawn => 'p',
                    PieceType.Knight => 'n',
                    PieceType.Bishop => 'b',
                    PieceType.Rook => 'r',
                    PieceType.Queen => 'q',
                    PieceType.King => 'k',
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (piece.Color == PieceColor.White)
                {
                    letter = char.ToUpper(letter);
                }

                if (skipLength > 0)
                {
                    fen.Append(skipLength);
                    skipLength = 0;
                }

                fen.Append(letter);
            }

            if (pos % 8 == 0 && pos != 0)
            {
                if (skipLength > 0)
                {
                    fen.Append(skipLength);
                    skipLength = 0;
                }

                fen.Append('/');
            }
        }

        fen.Append(' ');
        fen.Append(ColorToMove == PieceColor.White ? 'w' : 'b');
        fen.Append(' ');
        if (_castlingRights.CanCastle(PieceColor.White, CastleType.Kingside))
        {
            fen.Append('K');
        }

        if (_castlingRights.CanCastle(PieceColor.White, CastleType.Queenside))
        {
            fen.Append('Q');
        }

        if (_castlingRights.CanCastle(PieceColor.Black, CastleType.Kingside))
        {
            fen.Append('k');
        }

        if (_castlingRights.CanCastle(PieceColor.Black, CastleType.Queenside))
        {
            fen.Append('b');
        }

        fen.Append(' ');

        var enPassantPosition = GetEnPassantPosition();
        fen.Append(enPassantPosition != -1 ? GetSquareName(enPassantPosition) : '-');
        fen.Append($" {HalfMoves} {FullMoves}");

        return fen.ToString();
    }

    public int GetEnPassantPosition()
    {
        if (EnPassantFile == -1)
        {
            return -1;
        }

        return ColorToMove.Opposite() == PieceColor.Black ? 16 + EnPassantFile : 40 + EnPassantFile;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref Bitboard GetColorBitboardRef(PieceColor color)
    {
        return ref _colorBitboards[(int)color];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref Bitboard GetPieceBitboardRef(PieceType pieceType)
    {
        return ref _pieceBitboards[(int)pieceType];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard GetColorBitboard(PieceColor color)
    {
        return _colorBitboards[(int)color];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard GetPieceBitboard(PieceType pieceType)
    {
        return _pieceBitboards[(int)pieceType];
    }

    public static string GetSquareName(int position)
    {
        return (char)('a' + position % 8) + (8 - position / 8).ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsOfColorAt(int position, PieceColor color)
    {
        return _pieceColors[position] == color;
    }

    public readonly MoveGenerator MoveGenerator;

    public Board()
    {
        MoveGenerator = new MoveGenerator(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPieceAt(int square, Piece piece)
    {
        RemovePieceAt(square);

        if (piece.IsEmpty)
        {
            return;
        }

        var squareBitboard = Bitboard.WithSetBit(square);
        GetColorBitboardRef(piece.Color) |= squareBitboard;
        GetPieceBitboardRef(piece.Type) |= squareBitboard;
        _occupationBitboard |= squareBitboard;

        _pieceColors[square] = piece.Color;
        _pieceTypes[square] = piece.Type;

        PieceSquareEvaluator.FeedSetAt(square, piece);
        _zobrist.FeedPiece(square, piece);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsOfTypeAt(int square, PieceType pieceType)
    {
        return _pieceTypes[square] == pieceType;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemovePieceAt(int square)
    {
        var removedPiece = GetPieceAt(square);
        if (removedPiece.IsEmpty)
        {
            return;
        }

        PieceSquareEvaluator.FeedRemoveAt(square, removedPiece);
        _zobrist.FeedPiece(square, removedPiece);

        _pieceColors[square] = (PieceColor)byte.MaxValue;
        _pieceTypes[square] = (PieceType)byte.MaxValue;

        var negSquareBitboard = ~Bitboard.WithSetBit(square);

        for (var i = 0; i < _colorBitboards.Length; i++)
        {
            _colorBitboards[i] &= negSquareBitboard;
        }

        for (var i = 0; i < _pieceBitboards.Length; i++)
        {
            _pieceBitboards[i] &= negSquareBitboard;
        }

        _occupationBitboard &= negSquareBitboard;
    }

    private static readonly Dictionary<char, PieceType> PieceNotationMap = new()
    {
        { 'p', PieceType.Pawn }, { 'n', PieceType.Knight },
        { 'b', PieceType.Bishop }, { 'r', PieceType.Rook },
        { 'q', PieceType.Queen }, { 'k', PieceType.King }
    };

    public static int GetSquareFromName(string cellName)
    {
        return (8 - (cellName[1] - '0')) * 8 + (cellName[0] - 'a');
    }

    public void ParseFen(string text)
    {
        Clear();

        var split = text.Split();

        ColorToMove = split[1] == "w" ? PieceColor.White : PieceColor.Black;

        _castlingRights.SetCastleAllowed(PieceColor.Black, CastleType.Kingside, split[2].Contains('k'));
        _castlingRights.SetCastleAllowed(PieceColor.Black, CastleType.Queenside, split[2].Contains('q'));
        _castlingRights.SetCastleAllowed(PieceColor.White, CastleType.Kingside, split[2].Contains('K'));
        _castlingRights.SetCastleAllowed(PieceColor.White, CastleType.Queenside, split[2].Contains('Q'));

        var enPassantPosition = split[3] == "-" ? -1 : GetSquareFromName(split[3]);
        if (enPassantPosition == -1)
        {
            EnPassantFile = -1;
        }
        else
        {
            EnPassantFile = ColorToMove.Opposite() == PieceColor.Black
                ? enPassantPosition - 16
                : enPassantPosition - 40;
        }

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

        RecalculateBitboards();
        FeedZobristMove();
    }

    private void FeedZobristMove()
    {
        _zobrist.FeedCastlingRights(_castlingRights);
        _zobrist.FeedColorToPlay(ColorToMove);
        _zobrist.FeedEnPassantFile(EnPassantFile);
    }

    public void ResetToDefaultPosition()
    {
        ParseFen(StartFen);
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

        _attackedBitboardIgnoreKing = new ByColorIndexer<Bitboard>();
        _pinsInfo = new ByColorIndexer<PinsInfo>();

        _occupationBitboard.Reset();
        _checkersBitboard.Reset();

        HalfMoves = 0;
        FullMoves = 0;
        ColorToMove = PieceColor.Black;
        _castlingRights = new CastlingRights();
        EnPassantFile = -1;

        Array.Fill(_pieceColors, (PieceColor)0);
        Array.Fill(_pieceTypes, (PieceType)0);

        _cachedGameEndState = null;

        PieceSquareEvaluator.Reset();
        _zobrist.Reset();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetAllPieces(Span<int> destination, int start = 0)
    {
        return _occupationBitboard.BitScanForwardAll(destination, start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetAllPieces(Span<int> destination, PieceColor color, int start = 0)
    {
        var pieces = GetColorBitboard(color);
        return pieces.BitScanForwardAll(destination, start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Piece GetPieceAt(int position)
    {
        return IsEmpty(position)
            ? Piece.Empty
            : new Piece(GetColorAt(position), GetPieceTypeAt(position));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Piece GetPieceOfTypeAt(PieceType type, int position)
    {
        return IsEmpty(position)
            ? Piece.Empty
            : new Piece(GetColorAt(position), type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PieceColor GetColorAt(int position)
    {
        return _pieceColors[position];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PieceType GetPieceTypeAt(int position)
    {
        return _pieceTypes[position];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEmpty(int position)
    {
        return !IsOccupied(position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsOccupied(int position)
    {
        return _occupationBitboard.TestAt(position);
    }

    public GameEndState GetGameEndState()
    {
        if (_cachedGameEndState.HasValue)
        {
            return _cachedGameEndState.Value;
        }

        if (HalfMoves >= 50)
        {
            return (_cachedGameEndState = GameEndState.Draw).Value;
        }

        var isCheck = IsCheck();
        var canMove = MoveGenerator.GetLegalMoves().Length > 0;
        if (!canMove && !isCheck)
        {
            return (_cachedGameEndState = GameEndState.Draw).Value;
        }

        if (!canMove && isCheck)
        {
            return (_cachedGameEndState =
                ColorToMove == PieceColor.White ? GameEndState.BlackWin : GameEndState.WhiteWin).Value;
        }

        return (_cachedGameEndState = GameEndState.None).Value;
    }

    private GameEndState? _cachedGameEndState;

    public bool IsGameEnded()
    {
        return GetGameEndState() != GameEndState.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsCheck()
    {
        return _checkersBitboard != 0;
    }

    public bool IsCheckmate()
    {
        return IsCheck() && MoveGenerator.GetLegalMoves().Length == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetKingSquare(PieceColor color)
    {
        var king = GetPieceBitboard(PieceType.King) & GetColorBitboard(color);
        Debug.Assert(king.PopCount() == 1);
        return king.BitScanForward();
    }

    public void MakeMove(Move move)
    {
        Debug.Assert(IsOccupied(move.Start));

        ColorToMove = ColorToMove.Opposite();
        var undoHalfMoves = HalfMoves++;

        var capturedPiece = Piece.Empty;

        var castlingRights = _castlingRights;
        var enPassantPosition = EnPassantFile;

        var movingPiece = GetPieceAt(move.Start);

        if (movingPiece.Type == PieceType.Pawn)
        {
            HalfMoves = 0;
        }

        EnPassantFile = -1;
        RemovePieceAt(move.Start);

        switch (move.Type)
        {
            case MoveType.Quiet:
                break;
            case MoveType.Capture:
                capturedPiece = GetPieceAt(move.End);
                HalfMoves = 0;
                break;
            case MoveType.DoublePawn:
                EnPassantFile = movingPiece.Color == PieceColor.White ? move.Start - 48 : move.Start - 8;
                break;
            case MoveType.EnPassant:
                var capturedPosition = movingPiece.Color == PieceColor.White ? move.End + 8 : move.End - 8;
                capturedPiece = GetPieceAt(capturedPosition);
                RemovePieceAt(capturedPosition);
                break;
            case MoveType.KingsideCastle:
                RemovePieceAt(GetKingsideCastleRookStart(movingPiece.Color));
                SetPieceAt(GetKingsideCastleRookEnd(movingPiece.Color), new Piece(movingPiece.Color, PieceType.Rook));
                break;
            case MoveType.QueensideCastle:
                RemovePieceAt(GetQueensideCastleRookStart(movingPiece.Color));
                SetPieceAt(GetQueensideCastleRookEnd(movingPiece.Color), new Piece(movingPiece.Color, PieceType.Rook));
                break;
            case MoveType.KnightPromotionQuiet:
                movingPiece = new Piece(movingPiece.Color, PieceType.Knight);
                goto case MoveType.Quiet;
            case MoveType.BishopPromotionQuiet:
                movingPiece = new Piece(movingPiece.Color, PieceType.Bishop);
                goto case MoveType.Quiet;
            case MoveType.RookPromotionQuiet:
                movingPiece = new Piece(movingPiece.Color, PieceType.Rook);
                goto case MoveType.Quiet;
            case MoveType.QueenPromotionQuiet:
                movingPiece = new Piece(movingPiece.Color, PieceType.Queen);
                goto case MoveType.Quiet;
            case MoveType.KnightPromotionCapture:
                movingPiece = new Piece(movingPiece.Color, PieceType.Knight);
                goto case MoveType.Capture;
            case MoveType.BishopPromotionCapture:
                movingPiece = new Piece(movingPiece.Color, PieceType.Bishop);
                goto case MoveType.Capture;
            case MoveType.RookPromotionCapture:
                movingPiece = new Piece(movingPiece.Color, PieceType.Rook);
                goto case MoveType.Capture;
            case MoveType.QueenPromotionCapture:
                movingPiece = new Piece(movingPiece.Color, PieceType.Queen);
                goto case MoveType.Capture;
            default:
                Debug.Assert(false);
                throw new ArgumentOutOfRangeException();
        }

        SetPieceAt(move.End, movingPiece);
        UpdateCastlingRights(move, movingPiece, capturedPiece);

        if (movingPiece.Color == PieceColor.Black)
        {
            FullMoves++;
        }

        Debug.Assert(capturedPiece.IsEmpty || capturedPiece.Type != PieceType.King);

        _undoInfos.Push(new MoveUndoInfo
        {
            Move = move,
            CapturedPiece = capturedPiece,
            EnPassantFile = enPassantPosition,
            CastlingRights = castlingRights,
            HalfMoves = undoHalfMoves,
            Checkers = _checkersBitboard,
            AttackedIgnoreKing = _attackedBitboardIgnoreKing,
            Pins = _pinsInfo
        });

        RecalculateBitboards();
        _cachedGameEndState = null;

        FeedZobristMove();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateCastlingRights(Move move, Piece movedPiece, Piece capturedPiece)
    {
        if (!capturedPiece.IsEmpty)
        {
            if (GetKingsideCastleRookStart(capturedPiece.Color) == move.End)
            {
                _castlingRights.DisallowCastle(capturedPiece.Color, CastleType.Kingside);
            }

            if (GetQueensideCastleRookStart(capturedPiece.Color) == move.End)
            {
                _castlingRights.DisallowCastle(capturedPiece.Color, CastleType.Queenside);
            }
        }

        if (movedPiece.Type == PieceType.King)
        {
            _castlingRights.DisallowCastle(movedPiece.Color, CastleType.Queenside | CastleType.Kingside);
        }
        else if (movedPiece.Type == PieceType.Rook)
        {
            var disallowColor = move.Start is 63 or 56 ? PieceColor.White : PieceColor.Black;
            var disallowType = move.Start is 63 or 7 ? CastleType.Kingside : CastleType.Queenside;
            _castlingRights.DisallowCastle(disallowColor, disallowType);
        }
    }

    private void RecalculateBitboards()
    {
        var (whiteAttack, blackAttack) = MoveGenerator.GenerateAttackedByBitboardIgnoreKing();
        _attackedBitboardIgnoreKing.Set(PieceColor.White, whiteAttack);
        _attackedBitboardIgnoreKing.Set(PieceColor.Black, blackAttack);

        _pinsInfo.Set(PieceColor.White, MoveGenerator.GeneratePinsInfo(PieceColor.White));
        _pinsInfo.Set(PieceColor.Black, MoveGenerator.GeneratePinsInfo(PieceColor.Black));

        _checkersBitboard = MoveGenerator.GenerateCheckersBitboard();
    }

    public void RevertMove()
    {
        var undoInfo = _undoInfos.Pop();

        EnPassantFile = undoInfo.EnPassantFile;
        _castlingRights = undoInfo.CastlingRights;

        _checkersBitboard = undoInfo.Checkers;
        _attackedBitboardIgnoreKing = undoInfo.AttackedIgnoreKing;
        _pinsInfo = undoInfo.Pins;

        HalfMoves = undoInfo.HalfMoves;

        ColorToMove = ColorToMove.Opposite();

        var move = undoInfo.Move;

        var movedPiece = GetPieceAt(move.End);
        RemovePieceAt(move.End);

        var blackMoved = movedPiece.Color == PieceColor.Black;
        if (blackMoved)
        {
            FullMoves--;
        }

        switch (move.Type)
        {
            case MoveType.Quiet:
            case MoveType.DoublePawn:
                break;
            case MoveType.Capture:
                SetPieceAt(move.End, undoInfo.CapturedPiece);
                break;
            case MoveType.EnPassant:
                var capturedPosition = movedPiece.Color == PieceColor.White ? move.End + 8 : move.End - 8;
                SetPieceAt(capturedPosition, undoInfo.CapturedPiece);
                break;
            case MoveType.KingsideCastle:
                RemovePieceAt(GetKingsideCastleKingEnd(movedPiece.Color));
                RemovePieceAt(GetKingsideCastleRookEnd(movedPiece.Color));
                SetPieceAt(GetKingsideCastleRookStart(movedPiece.Color), new Piece(movedPiece.Color, PieceType.Rook));
                SetPieceAt(GetCastleKingStart(movedPiece.Color), movedPiece);
                break;
            case MoveType.QueensideCastle:
                RemovePieceAt(GetQueensideCastleKingEnd(movedPiece.Color));
                RemovePieceAt(GetQueensideCastleRookEnd(movedPiece.Color));
                SetPieceAt(GetQueensideCastleRookStart(movedPiece.Color), new Piece(movedPiece.Color, PieceType.Rook));
                SetPieceAt(GetCastleKingStart(movedPiece.Color), movedPiece);
                break;
            case MoveType.KnightPromotionQuiet:
            case MoveType.BishopPromotionQuiet:
            case MoveType.RookPromotionQuiet:
            case MoveType.QueenPromotionQuiet:
                movedPiece = new Piece(movedPiece.Color, PieceType.Pawn);
                break;
            case MoveType.BishopPromotionCapture:
            case MoveType.KnightPromotionCapture:
            case MoveType.RookPromotionCapture:
            case MoveType.QueenPromotionCapture:
                movedPiece = new Piece(movedPiece.Color, PieceType.Pawn);
                goto case MoveType.Capture;
            default:
                Debug.Assert(false);
                throw new ArgumentOutOfRangeException();
        }

        SetPieceAt(move.Start, movedPiece);
        _cachedGameEndState = null;

        FeedZobristMove();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetCastleKingStart(PieceColor color)
    {
        return color == PieceColor.Black ? 4 : 60;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetKingsideCastleKingEnd(PieceColor color)
    {
        return color == PieceColor.Black ? 6 : 62;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetQueensideCastleKingEnd(PieceColor color)
    {
        return color == PieceColor.Black ? 2 : 58;
    }

    private readonly Stack<MoveUndoInfo> _undoInfos = new(256);

    public Board(string fen) :
        this()
    {
        ParseFen(fen);
    }

    public Bitboard FreeBitboard => ~_occupationBitboard;
    private readonly ZobristHash _zobrist = new();
    public ulong ZobristHash => _zobrist.Value;

    public IEnumerable<int> GetAllPieces(PieceColor color)
    {
        var squares = new int[32];
        var count = GetAllPieces(squares, color);
        for (var i = 0; i < count; i++)
        {
            yield return squares[i];
        }
    }

    public IEnumerable<int> GetAllPieces()
    {
        var squares = new int[32];
        var count = GetAllPieces(squares);
        for (var i = 0; i < count; i++)
        {
            yield return squares[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetKingsideCastleRookStart(PieceColor color)
    {
        return color == PieceColor.Black ? 7 : 63;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetQueensideCastleRookStart(PieceColor color)
    {
        return color == PieceColor.Black ? 0 : 56;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetKingsideCastleRookEnd(PieceColor color)
    {
        return color == PieceColor.Black ? 5 : 61;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetQueensideCastleRookEnd(PieceColor color)
    {
        return color == PieceColor.Black ? 3 : 59;
    }

    public int GetEnPassantCapturedPawn(Move move)
    {
        Debug.Assert(move.Type == MoveType.EnPassant);
        var color = GetColorAt(move.Start);
        return color == PieceColor.Black ? move.End - 8 : move.End + 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetAllPieces(Span<int> destination, PieceType type, PieceColor color, int start = 0)
    {
        var pieces = GetColorBitboard(color) & GetPieceBitboard(type);
        return pieces.BitScanForwardAll(destination, start);
    }

    public DetailedMove GetDetailedMove(Move move)
    {
        var movedPiece = GetPieceAt(move.Start);
        var capturedPiece = GetPieceAt(move.Type == MoveType.EnPassant ? GetEnPassantCapturedPawn(move) : move.End);
        return new DetailedMove
        {
            Move = move,
            MovedPiece = movedPiece,
            CapturedPiece = capturedPiece
        };
    }
}