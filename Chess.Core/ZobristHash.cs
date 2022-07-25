namespace Chess.Core;

public class ZobristHash
{
    private static readonly Random Random = new(125754231);

    private static readonly ulong[] ColorsToMove;
    private static readonly ulong[] CastlingRights;
    private static readonly ulong[] EnPassantFile;
    private static readonly ByPieceIndexer<ulong[]> WhitePieceSquares;
    private static readonly ByPieceIndexer<ulong[]> BlackPieceSquares;

    private PieceColor? _lastFedColorToPlay;
    private int _lastEnPassantFile = -1;
    private CastlingRights? _lastCastlingRights;
    public ulong Value { get; private set; }

    static ZobristHash()
    {
        WhitePieceSquares.Set(PieceType.Pawn, GetRandomFilledArray(64));
        WhitePieceSquares.Set(PieceType.Knight, GetRandomFilledArray(64));
        WhitePieceSquares.Set(PieceType.Bishop, GetRandomFilledArray(64));
        WhitePieceSquares.Set(PieceType.Rook, GetRandomFilledArray(64));
        WhitePieceSquares.Set(PieceType.Queen, GetRandomFilledArray(64));
        WhitePieceSquares.Set(PieceType.King, GetRandomFilledArray(64));

        BlackPieceSquares.Set(PieceType.Pawn, GetRandomFilledArray(64));
        BlackPieceSquares.Set(PieceType.Knight, GetRandomFilledArray(64));
        BlackPieceSquares.Set(PieceType.Bishop, GetRandomFilledArray(64));
        BlackPieceSquares.Set(PieceType.Rook, GetRandomFilledArray(64));
        BlackPieceSquares.Set(PieceType.Queen, GetRandomFilledArray(64));
        BlackPieceSquares.Set(PieceType.King, GetRandomFilledArray(64));

        ColorsToMove = GetRandomFilledArray(2);
        CastlingRights = GetRandomFilledArray(4);
        EnPassantFile = GetRandomFilledArray(8);
    }

    private static ulong Random64()
    {
        return (ulong)(Random.NextInt64() ^ Random.NextInt64() ^ Random.NextInt64());
    }

    private static ulong[] GetRandomFilledArray(int size)
    {
        var array = new ulong[size];
        for (var i = 0; i < size; i++)
        {
            array[i] = Random64();
        }

        return array;
    }

    public void FeedPiece(int square, Piece piece)
    {
        var table = piece.Color == PieceColor.Black ? BlackPieceSquares : WhitePieceSquares;
        Value ^= table.Get(piece.Type)[square];
    }

    public void FeedColorToPlay(PieceColor color)
    {
        if (_lastFedColorToPlay == color)
        {
            return;
        }

        if (_lastFedColorToPlay.HasValue)
        {
            Value ^= ColorsToMove[(int)_lastFedColorToPlay.Value];
        }

        _lastFedColorToPlay = color;
        Value ^= ColorsToMove[(int)color];
    }

    public void FeedEnPassantFile(int file)
    {
        if (file == _lastEnPassantFile)
        {
            return;
        }

        if (_lastEnPassantFile != -1)
        {
            Value ^= EnPassantFile[_lastEnPassantFile];
        }

        _lastEnPassantFile = file;
        if (file != -1)
        {
            Value ^= EnPassantFile[file];
        }
    }

    public void FeedCastlingRights(CastlingRights castlingRights)
    {
        var lastCastlingRights = _lastCastlingRights.GetValueOrDefault();

        if (lastCastlingRights.CanCastle(PieceColor.Black, CastleType.Kingside) !=
            castlingRights.CanCastle(PieceColor.Black, CastleType.Kingside))
        {
            Value ^= CastlingRights[0];
        }

        if (lastCastlingRights.CanCastle(PieceColor.Black, CastleType.Queenside) !=
            castlingRights.CanCastle(PieceColor.Black, CastleType.Queenside))
        {
            Value ^= CastlingRights[1];
        }

        if (lastCastlingRights.CanCastle(PieceColor.White, CastleType.Kingside) !=
            castlingRights.CanCastle(PieceColor.White, CastleType.Kingside))
        {
            Value ^= CastlingRights[2];
        }

        if (lastCastlingRights.CanCastle(PieceColor.White, CastleType.Queenside) !=
            castlingRights.CanCastle(PieceColor.White, CastleType.Queenside))
        {
            Value ^= CastlingRights[3];
        }

        _lastCastlingRights = castlingRights;
    }

    public void Reset()
    {
        Value = 0;
        _lastCastlingRights = null;
        _lastFedColorToPlay = null;
        _lastEnPassantFile = -1;
    }
}