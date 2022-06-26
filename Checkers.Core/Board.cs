namespace Checkers.Core;

public class Board
{
    public static bool IsValidBoardSize(int size)
    {
        return size % 2 == 0 && size is >= 6 and <= 64;
    }

    public const int StandardSize = 8;

    public readonly int Size;
    public PieceColor CurrentTurn { get; private set; } = PieceColor.White;

    private readonly Dictionary<Position, Piece> _board = new();
    public readonly MoveGenerator MoveGenerator;

    public void SetPieceAt(Position position, Piece piece)
    {
        if (piece.IsEmpty)
        {
            _board.Remove(position);
        }
        else
        {
            _board[position] = piece;
        }
    }

    public Board(int size = StandardSize, bool reset = true)
    {
        if (!IsValidBoardSize(size))
        {
            throw new ArgumentException($"Invalid board size: {size}.");
        }

        Size = size;

        MoveGenerator = new MoveGenerator(this);

        if (reset)
        {
            Reset();
        }
    }

    public bool IsInBounds(Position position)
    {
        return position.X >= 0 && position.X < Size && position.Y >= 0 && position.Y < Size;
    }

    public void Reset()
    {
        _board.Clear();

        for (var x = 0; x < Size; x++)
        {
            for (var y = 0; y < Size; y++)
            {
                if ((x + y) % 2 == 0)
                {
                    continue;
                }

                if (y < Size / 2 - 1)
                {
                    _board[new Position(x, y)] = Piece.BlackPawn;
                }
                else if (y > Size / 2)
                {
                    _board[new Position(x, y)] = Piece.WhitePawn;
                }
            }
        }

        CurrentTurn = PieceColor.White;
        _stalemateTurns = 0;
        ClearCache();
    }

    public Board Copy()
    {
        var copy = new Board(Size, false);
        CopyTo(copy);
        return copy;
    }

    public void CopyTo(Board destination)
    {
        if (Size != destination.Size)
        {
            throw new ArgumentException($"Destination board Size: {destination.Size} != source board Size: {Size}.",
                nameof(destination));
        }

        destination._board.Clear();
        foreach (var piece in GetAllPieces())
        {
            destination._board[piece.Position] = piece.Piece;
        }

        destination._stalemateTurns = _stalemateTurns;
        destination.CurrentTurn = CurrentTurn;
    }

    public void SetState(BoardState state)
    {
        _board.Clear();

        foreach (var pieceOnBoard in state.Pieces)
        {
            _board[pieceOnBoard.Position] = pieceOnBoard.Piece;
        }

        _stalemateTurns = state.StalemateTurns;
        CurrentTurn = state.Turn;
        ClearCache();
    }

    public BoardState GetState()
    {
        return new BoardState
        {
            StalemateTurns = _stalemateTurns,
            Turn = CurrentTurn,
            Pieces = GetAllPieces().ToArray()
        };
    }

    public IEnumerable<PieceOnBoard> GetAllPieces()
    {
        return _board.Select(pair => new PieceOnBoard(pair.Key, pair.Value));
    }

    public Piece GetPieceAt(Position position)
    {
        return _board.TryGetValue(position, out var piece) ? piece : Piece.Empty;
    }

    public void MakeMove(Move move)
    {
        var piece = move.PieceOnBoard;
        _board.Remove(piece.Position);

        var pathEnd = move.Path.Last();
        var capturedSomething = false;

        foreach (var capturedPosition in MoveGenerator.GetAllMoveCaptures(move))
        {
            capturedSomething = true;
            _board.Remove(capturedPosition);
        }

        var hasPromoted = MoveGenerator.ShouldPromote(move.Path, move.PieceOnBoard.Piece.Color);
        var newPiece = hasPromoted
            ? new Piece(PieceType.Queen, piece.Piece.Color)
            : piece.Piece;
        _board[pathEnd] = newPiece;

        if (piece.Piece.Type == PieceType.Queen && !capturedSomething)
        {
            _stalemateTurns++;
        }
        else
        {
            _stalemateTurns = 0;
        }

        CurrentTurn = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
        ClearCache();
    }

    private void ClearCache()
    {
        _cachedGameEndState = null;
        MoveGenerator.ClearCache();
    }

    private const int TurnsBeforeStalemate = 10;

    private int _stalemateTurns;
    private GameEndState? _cachedGameEndState;


    public GameEndState GetGameEndState()
    {
        if (_cachedGameEndState.HasValue)
        {
            return _cachedGameEndState.Value;
        }

        var pieces = GetAllPieces().ToArray();
        var whitePieces = pieces.Count(p => p.Piece.Color == PieceColor.White);
        var blackPieces = pieces.Length - whitePieces;

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

        var allMoves = MoveGenerator.GenerateAllMoves();
        var whiteMoveCount = allMoves.Count(m => m.PieceOnBoard.Piece.Color == PieceColor.White);
        var blackMoveCount = allMoves.Count - whiteMoveCount;

        if (whiteMoveCount == 0 && CurrentTurn == PieceColor.White)
        {
            return (_cachedGameEndState = GameEndState.BlackWin).Value;
        }

        if (blackMoveCount == 0 && CurrentTurn == PieceColor.Black)
        {
            return (_cachedGameEndState = GameEndState.WhiteWin).Value;
        }

        return (_cachedGameEndState = _stalemateTurns >= TurnsBeforeStalemate ? GameEndState.Draw : GameEndState.None)
            .Value;
    }

    public bool IsGameEnded()
    {
        return GetGameEndState() != GameEndState.None;
    }
}