namespace Checkers;

public class Board
{
    public const int StandardSize = 10;
    public readonly int Size;
    public PieceColor CurrentTurn { get; private set; } = PieceColor.White;

    private readonly Piece[,] _board;

    public readonly MoveGenerator MoveGenerator;

    public Board(int size = StandardSize, bool reset = true)
    {
        if (size % 2 == 1)
        {
            throw new ArgumentException($"Board size must be even. Was {size}.");
        }

        Size = size;
        _board = new Piece[Size, Size];

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
        foreach (var position in GetAllBoardPositions())
        {
            var (x, y) = (position.X, position.Y);
            var piece = Piece.Empty;

            if ((x + y) % 2 == 0)
            {
                _board[x, y] = piece;
                continue;
            }

            if (y < Size / 2 - 1)
            {
                piece = Piece.BlackPawn;
            }
            else if (y > Size / 2)
            {
                piece = Piece.WhitePawn;
            }

            _board[x, y] = piece;
        }

        CurrentTurn = PieceColor.White;
        MoveGenerator.ResetMoves();
    }

    public Board Clone()
    {
        var clone = new Board(Size, false);
        clone.SetState(GetState());
        return clone;
    }

    private IEnumerable<Position> GetAllBoardPositions()
    {
        return Enumerable.Range(0, Size)
            .SelectMany(x => Enumerable.Range(0, Size)
                .Select(y => new Position(x, y)));
    }

    public void SetState(BoardState state)
    {
        foreach (var position in GetAllBoardPositions())
        {
            _board[position.X, position.Y] = Piece.Empty;
        }

        foreach (var pieceOnBoard in state.Pieces)
        {
            _board[pieceOnBoard.Position.X, pieceOnBoard.Position.Y] = pieceOnBoard.Piece;
        }

        _stalemateTurns = state.StalemateTurns;
        CurrentTurn = state.Turn;
        MoveGenerator.ResetMoves();
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

    public override int GetHashCode()
    {
        var hashCode = 0;
        foreach (var piece in GetAllPieces())
        {
            hashCode = HashCode.Combine(hashCode, piece);
        }

        return hashCode;
    }

    public IEnumerable<PieceOnBoard> GetAllPieces()
    {
        for (var x = 0; x < Size; x++)
        {
            for (var y = 0; y < Size; y++)
            {
                var position = new Position(x, y);
                var piece = GetPieceAt(position);
                if (!piece.IsEmpty)
                {
                    yield return new PieceOnBoard(position, piece);
                }
            }
        }
    }

    public Piece GetPieceAt(Position position)
    {
        return _board[position.X, position.Y];
    }

    public void MakeMove(Move move)
    {
        var piece = move.PieceOnBoard;
        _board[piece.Position.X, piece.Position.Y] = Piece.Empty;

        var pathEnd = move.Path[^1];
        var capturedSomething = false;
        foreach (var capturedPosition in MoveGenerator.GetAllMoveCaptures(move))
        {
            capturedSomething = true;
            _board[capturedPosition.X, capturedPosition.Y] = Piece.Empty;
        }

        var hasPromoted = MoveGenerator.ShouldPromote(move.Path, move.PieceOnBoard.Piece.Color);
        var newPiece = hasPromoted
            ? new Piece(PieceType.Queen, piece.Piece.Color)
            : piece.Piece;
        _board[pathEnd.X, pathEnd.Y] = newPiece;

        if (capturedSomething || hasPromoted)
        {
            _stalemateTurns = 0;
        }
        else
        {
            _stalemateTurns++;
        }

        CurrentTurn = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
        MoveGenerator.ResetMoves();
    }

    private int _stalemateTurns;

    public GameEndState GetGameEndState()
    {
        var pieces = GetAllPieces().ToArray();
        var whitePieces = pieces.Count(p => p.Piece.Color == PieceColor.White);
        var blackPieces = pieces.Length - whitePieces;

        if (whitePieces == 0 && blackPieces == 0)
        {
            return GameEndState.None;
        }

        if (whitePieces == 0)
        {
            return GameEndState.BlackWin;
        }

        if (blackPieces == 0)
        {
            return GameEndState.WhiteWin;
        }

        var allMoves = MoveGenerator.GenerateAllMoves();
        var whiteMoveCount = allMoves.Count(m => m.PieceOnBoard.Piece.Color == PieceColor.White);
        var blackMoveCount = allMoves.Count - whiteMoveCount;

        if (whiteMoveCount == 0 && CurrentTurn == PieceColor.White)
        {
            return GameEndState.BlackWin;
        }

        if (blackMoveCount == 0 && CurrentTurn == PieceColor.Black)
        {
            return GameEndState.WhiteWin;
        }

        return _stalemateTurns >= StalemateTurns ? GameEndState.Draw : GameEndState.None;
    }

    private const int StalemateTurns = 60;
}

public enum GameEndState
{
    None,
    Draw,
    WhiteWin,
    BlackWin
}

public class BoardState
{
    public int StalemateTurns { get; init; }
    public PieceColor Turn { get; init; }
    public IReadOnlyList<PieceOnBoard> Pieces { get; init; } = null!;
}