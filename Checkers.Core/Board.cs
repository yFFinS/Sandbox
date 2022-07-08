using System.Runtime.CompilerServices;

namespace Checkers.Core;

public class Board
{
    public static bool IsValidBoardSize(int size)
    {
        return size % 2 == 0 && size is >= 6 and <= 64;
    }

    public const int MaxTurns = 200;
    public const int StandardSize = 8;

    public int TurnCount { get; private set; }

    public readonly int Size;
    public PieceColor CurrentTurn { get; private set; } = PieceColor.White;

    private readonly Piece[,] _board;
    public readonly MoveGenerator MoveGenerator;

    public static string GetCellName(Position position)
    {
        if (position.X is < 0 or >= StandardSize)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        return (char)('a' + position.X) + (StandardSize - position.Y).ToString();
    }

    public Board(int size = StandardSize, bool reset = true)
    {
        if (!IsValidBoardSize(size))
        {
            throw new ArgumentException($"Invalid board size: {size}.");
        }

        Size = size;
        _board = new Piece[Size, Size];

        MoveGenerator = new MoveGenerator(this);

        if (reset)
        {
            Reset();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void SetPieceAt(Position position, Piece piece)
    {
        _board[position.X, position.Y] = piece;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool IsInBounds(in Position position)
    {
        return position.X >= 0 && position.X < Size && position.Y >= 0 && position.Y < Size;
    }

    public void Reset()
    {
        for (var x = 0; x < Size; x++)
        {
            for (var y = 0; y < Size; y++)
            {
                if ((x + y) % 2 == 0)
                {
                    _board[x, y] = Piece.Empty;
                    continue;
                }

                if (y < Size / 2 - 1)
                {
                    _board[x, y] = Piece.BlackPawn;
                }
                else if (y > Size / 2)
                {
                    _board[x, y] = Piece.WhitePawn;
                }
                else
                {
                    _board[x, y] = Piece.Empty;
                }
            }
        }

        CurrentTurn = PieceColor.White;
        StalemateTurns = 0;
        TurnCount = 0;
        ClearCache();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Board Copy()
    {
        var copy = new Board(Size, false);
        CopyTo(copy);
        return copy;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void CopyTo(Board destination)
    {
        if (Size != destination.Size)
        {
            throw new ArgumentException($"Destination board Size: {destination.Size} != source board Size: {Size}.",
                nameof(destination));
        }

        for (var x = 0; x < Size; x++)
        {
            for (var y = 0; y < Size; y++)
            {
                destination._board[x, y] = _board[x, y];
            }
        }

        destination.TurnCount = TurnCount;
        destination.StalemateTurns = StalemateTurns;
        destination.CurrentTurn = CurrentTurn;
    }

    public void Clear()
    {
        for (var x = 0; x < Size; x++)
        {
            for (var y = 0; y < Size; y++)
            {
                _board[x, y] = Piece.Empty;
            }
        }
    }

    public void SetState(BoardState state)
    {
        Clear();

        foreach (var pieceOnBoard in state.Pieces)
        {
            _board[pieceOnBoard.Position.X, pieceOnBoard.Position.Y] = pieceOnBoard.Piece;
        }

        StalemateTurns = state.StalemateTurns;
        TurnCount = state.TurnCount;
        CurrentTurn = state.Turn;
        ClearCache();
    }

    public BoardState GetState()
    {
        return new BoardState
        {
            StalemateTurns = StalemateTurns,
            Turn = CurrentTurn,
            TurnCount = TurnCount,
            Pieces = GetAllPieces().ToArray()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public IEnumerable<PieceOnBoard> GetAllPieces()
    {
        for (var x = 0; x < Size; x++)
        {
            for (var y = 0; y < Size; y++)
            {
                var piece = _board[x, y];
                if (!piece.IsEmpty)
                {
                    yield return new PieceOnBoard(new Position(x, y), piece);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal int GetAllPiecesNonAlloc(IList<PieceOnBoard> buffer)
    {
        var bufferIndex = 0;
        for (var x = 0; x < Size; x++)
        {
            for (var y = 0; y < Size; y++)
            {
                var piece = _board[x, y];
                if (!piece.IsEmpty)
                {
                    buffer[bufferIndex++] = new PieceOnBoard(new Position(x, y), piece);
                }
            }
        }

        return bufferIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Piece GetPieceAt(in Position position) => GetPieceAt(position.X, position.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Piece GetPieceAt(int x, int y)
    {
        return _board[x, y];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool IsEmpty(in Position position)
    {
        return GetPieceAt(position).IsEmpty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool IsEmpty(int x, int y)
    {
        return GetPieceAt(x, y).IsEmpty;
    }

    public void MakeMove(in Move move)
    {
        TurnCount++;

        var piece = move.PieceOnBoard;
        SetPieceAt(piece.Position, Piece.Empty);

        var pathEnd = move.Path[^1];
        var capturedSomething = false;

        foreach (var capturedPosition in MoveGenerator.GetAllMoveCaptures(move))
        {
            capturedSomething = true;
            SetPieceAt(capturedPosition, Piece.Empty);
        }

        var hasPromoted = MoveGenerator.ShouldPromote(move.Path, move.PieceOnBoard.Piece.Color);
        var newPiece = hasPromoted
            ? new Piece(PieceType.Queen, piece.Piece.Color)
            : piece.Piece;
        SetPieceAt(pathEnd, newPiece);

        if (piece.Piece.Type == PieceType.Queen && !capturedSomething)
        {
            StalemateTurns++;
        }
        else
        {
            StalemateTurns = 0;
        }

        CurrentTurn = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
        ClearCache();
    }

    private void ClearCache()
    {
        _cachedGameEndState = null;
        MoveGenerator.ClearCache();
    }

    public const int MaxStalemateTurns = 10;

    public int StalemateTurns { get; private set; }
    private GameEndState? _cachedGameEndState;


    public GameEndState GetGameEndState()
    {
        if (_cachedGameEndState.HasValue)
        {
            return _cachedGameEndState.Value;
        }

        var whitePieces = 0;
        var blackPieces = 0;
        foreach (var piece in GetAllPieces())
        {
            if (piece.Piece.Color == PieceColor.White)
            {
                whitePieces++;
            }
            else
            {
                blackPieces++;
            }
        }

        if (whitePieces == 0 && blackPieces == 0)
        {
            return (_cachedGameEndState = GameEndState.None).Value;
        }

        if (whitePieces == 0)
        {
            return (_cachedGameEndState = GameEndState.BlackWin).Value;
        }

        if (blackPieces == 0)
        {
            return (_cachedGameEndState = GameEndState.WhiteWin).Value;
        }

        var canMove = MoveGenerator.HasAnyMove(CurrentTurn);

        if (!canMove && CurrentTurn == PieceColor.White)
        {
            return (_cachedGameEndState = GameEndState.BlackWin).Value;
        }

        if (!canMove && CurrentTurn == PieceColor.Black)
        {
            return (_cachedGameEndState = GameEndState.WhiteWin).Value;
        }

        if (TurnCount >= MaxTurns || StalemateTurns >= MaxStalemateTurns)
        {
            return (_cachedGameEndState = GameEndState.Draw).Value;
        }

        return (_cachedGameEndState = GameEndState.None).Value;
    }

    public bool IsGameEnded()
    {
        return GetGameEndState() != GameEndState.None;
    }
}