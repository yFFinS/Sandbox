using System.Drawing;
using System.Runtime.CompilerServices;

namespace Checkers.Core;

public class MoveGenerator
{
    private static readonly Point[] Directions = { new(-1, -1), new(-1, 1), new(1, -1), new(1, 1) };

    private readonly Board _board;
    private IReadOnlyList<Move>? _cachedMoves;
    private bool _cachingEnabled;

    public MoveGenerator(Board board, bool cachingEnabled = true)
    {
        _board = board;
        _cachingEnabled = cachingEnabled;
        _cachedMoves = null;

        var rowsWithPieces = _board.Size - 2;
        var piecesPerRow = _board.Size / 2;
        _pieceBuffer = new PieceOnBoard[rowsWithPieces * piecesPerRow];
    }

    public void ClearCache()
    {
        _cachedMoves = null;
    }

    public void DisableCaching()
    {
        _cachingEnabled = false;
    }

    public void EnableCaching()
    {
        _cachingEnabled = true;
    }

    public IReadOnlyList<Move> GenerateAllMoves(bool onlyCurrentTurn = true)
    {
        if (_cachingEnabled && !onlyCurrentTurn)
        {
            return _cachedMoves ??= RecalculateAllValidMoves(true);
        }

        return RecalculateAllValidMoves(onlyCurrentTurn);
    }

    private List<Move> RecalculateAllValidMoves(bool onlyCurrentTurn)
    {
        var hasCapturingMove = false;
        var moves = new List<Move>();

        foreach (var move in GenerateAllMovesDoesNotRespectCapture(onlyCurrentTurn))
        {
            var isCapturing = IsCapturingMove(move);
            if (!hasCapturingMove && isCapturing)
            {
                hasCapturingMove = true;
                var tempMoves = new List<Move>(moves.Count);
                foreach (var tempMove in moves)
                {
                    if (IsCapturingMove(tempMove))
                    {
                        tempMoves.Add(tempMove);
                    }
                }

                moves = tempMoves;
            }

            if (!hasCapturingMove || hasCapturingMove && isCapturing)
            {
                moves.Add(move);
            }
        }

        return hasCapturingMove ? GetCorrectQueenMoves(moves) : moves;
    }

    private List<Move> GetCorrectQueenMoves(IReadOnlyList<Move> capturingMoves)
    {
        var correctMoves = new List<Move>();
        var queenGroups = new Dictionary<Position, List<Move>>();

        foreach (var capturingMove in capturingMoves)
        {
            var piece = capturingMove.PieceOnBoard.Piece;
            if (piece.Type != PieceType.Queen && !ShouldPromote(capturingMove.Path, piece.Color))
            {
                correctMoves.Add(capturingMove);
                continue;
            }

            var position = capturingMove.PieceOnBoard.Position;
            if (!queenGroups.TryGetValue(position, out var positionQueenMoves))
            {
                positionQueenMoves = new List<Move>();
                queenGroups[position] = positionQueenMoves;
            }

            positionQueenMoves.Add(capturingMove);
        }

        IEnumerable<Move> FindCorrectMoves(IReadOnlyList<Move> moves, int pathLength)
        {
            var filteredMoves = new List<Move>();
            foreach (var move in moves)
            {
                if (move.Path.Count > pathLength)
                {
                    filteredMoves.Add(move);
                }
            }

            if (filteredMoves.Count == 0)
            {
                foreach (var move in moves)
                {
                    yield return move;
                }

                yield break;
            }

            var collidingGroups = GroupMovesByPositionCollision(moves, pathLength);
            foreach (var collidingMoves in collidingGroups.Values)
            {
                foreach (var correctMove in FindCorrectMoves(collidingMoves, pathLength + 1))
                {
                    yield return correctMove;
                }
            }
        }

        foreach (var queenMoves in queenGroups.Values)
        {
            correctMoves.AddRange(FindCorrectMoves(queenMoves, 1));
        }

        return correctMoves;
    }

    private static Dictionary<Position, List<Move>> GroupMovesByPositionCollision(IReadOnlyList<Move> moves,
        int pathLength)
    {
        var groups = new Dictionary<Position, List<Move>>();
        foreach (var move in moves)
        {
            var position = move.Path[pathLength - 1];
            if (!groups.TryGetValue(position, out var collidingMoves))
            {
                collidingMoves = new List<Move> { move };
                groups[position] = collidingMoves;
            }
            else
            {
                collidingMoves.Add(move);
            }
        }

        return groups;
    }

    private IEnumerable<Move> GenerateAllMovesDoesNotRespectCapture(bool onlyCurrentTurn = true)
    {
        var pieces = new List<PieceOnBoard>();
        foreach (var pieceOnBoard in _board.GetAllPieces())
        {
            if (onlyCurrentTurn && pieceOnBoard.Piece.Color == _board.CurrentTurn || !onlyCurrentTurn)
            {
                pieces.Add(pieceOnBoard);
            }
        }

        foreach (var move in pieces.SelectMany(GenerateMovesForPieceDoesNotRespectCapture))
        {
            yield return move;
        }
    }

    public IEnumerable<Move> GenerateMovesForPiece(PieceOnBoard piece)
    {
        var moves = GenerateAllMoves();
        return moves.Where(move => move.PieceOnBoard.Position == piece.Position);
    }

    private IEnumerable<Move> GenerateMovesForPieceDoesNotRespectCapture(PieceOnBoard piece)
    {
        var pieceColor = piece.Piece.Color;
        var position = piece.Position;
        var isQueen = piece.Piece.Type == PieceType.Queen;
        var nonCaptureVerticalDirectionForPawn = pieceColor == PieceColor.Black ? 1 : -1;

        foreach (var direction in Directions)
        {
            var dx = direction.X;
            var dy = direction.Y;

            for (var moveDistance = 1; moveDistance <= (isQueen ? _board.Size : 2); moveDistance++)
            {
                var target = position.OffsetBy(dx * moveDistance, dy * moveDistance);
                if (!_board.IsInBounds(target))
                {
                    break;
                }

                var isWayClear = true;
                for (var distance = 1; distance <= moveDistance; distance++)
                {
                    if (_board.IsEmpty(position.X + distance * dx, position.Y + distance * dy))
                    {
                        continue;
                    }

                    isWayClear = false;
                    break;
                }

                if (isWayClear && (isQueen || (moveDistance == 1 && dy == nonCaptureVerticalDirectionForPawn)))
                {
                    yield return new Move(piece, new[] { target });
                }
                else if (moveDistance > 1 && TryGetCapturePosition(piece, position, target, out var capturePosition))
                {
                    var traversedPath = new List<Position> { target };
                    var ignored = new List<Position> { position, capturePosition, target };
                    foreach (var path in GenerateFullCapturePaths(piece, traversedPath, ignored))
                    {
                        yield return new Move(piece, path);
                    }
                }
            }
        }
    }

    [Flags]
    public enum MovementFlags
    {
        NonCapturing = 1 << 0,
        Capturing = 1 << 1,
        Any = NonCapturing | Capturing
    }

    public bool CanPieceMove(PieceOnBoard piece, MovementFlags flags = MovementFlags.Any)
    {
        var pieceColor = piece.Piece.Color;
        var position = piece.Position;
        var isQueen = piece.Piece.Type == PieceType.Queen;
        var nonCaptureVerticalDirectionForPawn = pieceColor == PieceColor.Black ? 1 : -1;

        var startMoveDistance = (flags & MovementFlags.NonCapturing) != 0 ? 1 : 2;
        var endMoveDistance = (flags & MovementFlags.Capturing) == 0 ? 1 : 2;

        foreach (var direction in Directions)
        {
            var dx = direction.X;
            var dy = direction.Y;

            for (var moveDistance = startMoveDistance; moveDistance <= endMoveDistance; moveDistance++)
            {
                var target = position.OffsetBy(dx * moveDistance, dy * moveDistance);
                if (!_board.IsInBounds(target))
                {
                    break;
                }

                var isWayClear = true;
                var wayPosition = new Position(position.X, position.Y);
                for (var distance = 1; distance <= moveDistance; distance++)
                {
                    wayPosition = wayPosition.OffsetBy(dx, dy);
                    if (_board.IsEmpty(wayPosition))
                    {
                        continue;
                    }

                    isWayClear = false;
                    break;
                }

                if (isWayClear && (isQueen || dy == nonCaptureVerticalDirectionForPawn))
                {
                    return true;
                }

                if (moveDistance > 1 && TryGetCapturePosition(piece, position, target, out _))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private Position? TryGetQueenPromotionPosition(IReadOnlyList<Position> traversedPath, in PieceColor color)
    {
        foreach (var position in traversedPath)
        {
            if (ShouldPromote(position, color))
            {
                return position;
            }
        }

        return null;
    }

    private IEnumerable<List<Position>> GenerateFullCapturePaths(PieceOnBoard piece,
        List<Position> traversedPath,
        List<Position> ignored)
    {
        var canMoveFurther = false;
        var position = traversedPath.Last();
        var isQueen = piece.Piece.Type == PieceType.Queen || ShouldPromote(traversedPath, piece.Piece.Color);
        foreach (var direction in Directions)
        {
            var dx = direction.X;
            var dy = direction.Y;

            for (var moveDistance = 2; moveDistance <= (isQueen ? _board.Size : 2); moveDistance++)
            {
                var endPosition = position.OffsetBy(dx, dy, moveDistance);
                if (!_board.IsInBounds(endPosition))
                {
                    break;
                }

                if (!TryGetCapturePosition(piece, position, endPosition, out var capturePosition, ignored))
                {
                    continue;
                }

                canMoveFurther = true;
                var newPath = traversedPath.Append(endPosition).ToList();
                var newIgnored = new List<Position>(ignored)
                {
                    capturePosition,
                    endPosition
                };

                foreach (var path in GenerateFullCapturePaths(piece, newPath, newIgnored))
                {
                    yield return path;
                }
            }
        }

        if (!canMoveFurther)
        {
            yield return traversedPath;
        }
    }

    private bool IsCapturingMove(in Move move)
    {
        return GetAllMoveCaptures(move).Any();
    }

    public List<Position> GetAllMoveCaptures(in Move move)
    {
        var captures = new List<Position>();
        var currentPosition = move.PieceOnBoard.Position;

        foreach (var position in move.Path)
        {
            if (TryGetCapturePosition(move.PieceOnBoard, currentPosition, position,
                    out var capturePosition, captures))
            {
                captures.Add(capturePosition);
            }

            currentPosition = position;
        }

        return captures;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private bool ShouldPromote(in Position currentPosition, in PieceColor color)
    {
        return currentPosition.Y == 0 && color == PieceColor.White ||
               currentPosition.Y == _board.Size - 1 && color == PieceColor.Black;
    }

    public bool ShouldPromote(IReadOnlyList<Position> traversedPath, in PieceColor color)
    {
        return TryGetQueenPromotionPosition(traversedPath, color).HasValue;
    }

    private bool TryGetCapturePosition(PieceOnBoard piece, in Position from, in Position to,
        out Position capturePosition,
        List<Position>? ignored = null)
    {
        bool IsIgnored(in Position position)
        {
            return piece.Position == position || ignored is not null && ignored.Contains(position);
        }

        capturePosition = new Position();
        var pieceAtTo = _board.GetPieceAt(to);
        if (!_board.IsInBounds(to) || (!pieceAtTo.IsEmpty && !IsIgnored(to)))
        {
            return false;
        }

        var (dx, dy) = from.DirectionTo(to);
        var moveDistance = from.DistanceTo(to);

        var captures = 0;
        var captured = new PieceOnBoard();
        var wayPosition = new Position(from.X, from.Y);
        for (var distance = 1; distance <= moveDistance; distance++)
        {
            wayPosition = wayPosition.OffsetBy(dx, dy);

            var pieceOnWay = _board.GetPieceAt(wayPosition);
            if (pieceOnWay.IsEmpty || IsIgnored(wayPosition))
            {
                continue;
            }

            captured = new PieceOnBoard(wayPosition, pieceOnWay);
            captures++;
            if (captures > 1)
            {
                break;
            }
        }

        if (captures != 1)
        {
            return false;
        }

        capturePosition = captured.Position;
        return captured.Piece.Color != piece.Piece.Color;
    }

    public MoveInfo GetMoveInfo(in Move move)
    {
        var capturedPositions = GetAllMoveCaptures(move).ToArray();
        var promotionPosition = TryGetQueenPromotionPosition(move.Path, move.PieceOnBoard.Piece.Color);
        return new MoveInfo(move, capturedPositions, promotionPosition);
    }

    private readonly PieceOnBoard[] _pieceBuffer;

    public bool HasAnyMove(in PieceColor color)
    {
        var count = _board.GetAllPiecesNonAlloc(_pieceBuffer);
        for (var i = 0; i < count; i++)
        {
            var pieceOnBoard = _pieceBuffer[i];
            if (pieceOnBoard.Piece.Color == color && CanPieceMove(pieceOnBoard))
            {
                return true;
            }
        }

        return false;
    }
}