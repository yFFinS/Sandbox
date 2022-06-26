using System.Drawing;

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
            if (!hasCapturingMove && IsCapturingMove(move))
            {
                hasCapturingMove = true;
                moves = moves.Where(IsCapturingMove).ToList();
            }

            if (!hasCapturingMove || hasCapturingMove && IsCapturingMove(move))
            {
                moves.Add(move);
            }
        }

        return hasCapturingMove ? FilterIncorrectQueenMoves(moves).ToList() : moves;
    }

    private IEnumerable<Move> FilterIncorrectQueenMoves(IEnumerable<Move> capturingMoves)
    {
        var queenMoves = new Dictionary<Position, List<Move>>();

        foreach (var capturingMove in capturingMoves)
        {
            var piece = capturingMove.PieceOnBoard.Piece;
            if (piece.Type != PieceType.Queen && !ShouldPromote(capturingMove.Path, piece.Color))
            {
                yield return capturingMove;
                continue;
            }

            var position = capturingMove.PieceOnBoard.Position;
            if (!queenMoves.TryGetValue(position, out var positionQueenMoves))
            {
                positionQueenMoves = new List<Move>();
                queenMoves[position] = positionQueenMoves;
            }

            positionQueenMoves.Add(capturingMove);
        }

        IEnumerable<Move> FindCorrectMoves(IReadOnlyList<Move> moves, int pathLength)
        {
            bool PathDoesNotEnded(Move move) => move.Path.Count > pathLength;

            var filteredMoves = moves.Where(PathDoesNotEnded).ToArray();
            if (filteredMoves.Length == 0)
            {
                foreach (var move in moves)
                {
                    yield return move;
                }
            }

            var groups = filteredMoves.GroupBy(move => move.Path[pathLength - 1])
                .ToDictionary(g => g.Key, g => g.ToArray());
            foreach (var move in groups.SelectMany(group =>
                         FindCorrectMoves(group.Value, pathLength + 1)))
            {
                yield return move;
            }
        }

        foreach (var correctMove in queenMoves.Values.SelectMany(moves => FindCorrectMoves(moves, 1)))
        {
            yield return correctMove;
        }
    }

    private IEnumerable<Move> GenerateAllMovesDoesNotRespectCapture(bool onlyCurrentTurn = true)
    {
        var allPieces = _board.GetAllPieces();
        var pieceOnBoards =
            onlyCurrentTurn ? allPieces.Where(piece => piece.Piece.Color == _board.CurrentTurn) : allPieces;
        foreach (var pieceOnBoard in pieceOnBoards)
        {
            foreach (var move in GenerateMovesForPieceDoesNotRespectCapture(pieceOnBoard))
            {
                yield return move;
            }
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
                var target = position.Offset(dx * moveDistance, dy * moveDistance);
                if (!_board.IsInBounds(target))
                {
                    break;
                }

                var isWayClear = true;
                var wayPosition = new Position(position.X, position.Y);
                for (var distance = 1; distance <= moveDistance; distance++)
                {
                    wayPosition.X += dx;
                    wayPosition.Y += dy;
                    var pieceOnWay = _board.GetPieceAt(wayPosition);
                    if (!pieceOnWay.IsEmpty)
                    {
                        isWayClear = false;
                        break;
                    }
                }

                if (isWayClear && (isQueen || (moveDistance == 1 && dy == nonCaptureVerticalDirectionForPawn)))
                {
                    yield return new Move(piece, new[] { target });
                }
                else if (moveDistance > 1 &&
                         TryGetCapturePosition(piece.Piece, position, target, out var capturePosition))
                {
                    var traversedPath = new List<Position> { target };
                    var ignored = new HashSet<Position> { position, capturePosition, target };
                    foreach (var path in GenerateFullCapturePaths(piece.Piece, traversedPath, ignored))
                    {
                        yield return new Move(piece, path);
                    }
                }
            }
        }
    }

    private Position? TryGetQueenPromotionPosition(IEnumerable<Position> traversedPath, PieceColor color)
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

    private IEnumerable<List<Position>> GenerateFullCapturePaths(Piece piece,
        List<Position> traversedPath,
        ISet<Position> ignored)
    {
        var canMoveFurther = false;
        var position = traversedPath.Last();
        var isQueen = piece.Type == PieceType.Queen || ShouldPromote(traversedPath, piece.Color);
        foreach (var direction in Directions)
        {
            var dx = direction.X;
            var dy = direction.Y;

            for (var moveDistance = 2; moveDistance <= (isQueen ? _board.Size : 2); moveDistance++)
            {
                var endPosition = position.Offset(dx, dy, moveDistance);
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
                var newIgnored = new HashSet<Position>(ignored)
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

    public bool IsCapturingMove(Move move)
    {
        return GetAllMoveCaptures(move).Any();
    }

    public IEnumerable<Position> GetAllMoveCaptures(Move move)
    {
        var ignored = new HashSet<Position> { move.PieceOnBoard.Position };
        var currentPosition = move.PieceOnBoard.Position;
        foreach (var position in move.Path)
        {
            if (TryGetCapturePosition(move.PieceOnBoard.Piece, currentPosition, position,
                    out var capturePosition, ignored))
            {
                ignored.Add(capturePosition);
                yield return capturePosition;
            }

            currentPosition = position;
        }
    }

    public bool ShouldPromote(Position currentPosition, PieceColor color)
    {
        return currentPosition.Y == 0 && color == PieceColor.White ||
               currentPosition.Y == _board.Size - 1 && color == PieceColor.Black;
    }

    public bool ShouldPromote(IEnumerable<Position> traversedPath, PieceColor color)
    {
        return TryGetQueenPromotionPosition(traversedPath, color).HasValue;
    }

    private bool TryGetCapturePosition(Piece piece, Position from, Position to, out Position capturePosition,
        ISet<Position>? ignored = null)
    {
        bool IsIgnored(Position position)
        {
            return ignored is not null && ignored.Contains(position);
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
            wayPosition.X += dx;
            wayPosition.Y += dy;
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
        return captured.Piece.Color != piece.Color;
    }

    public MoveFullInfo GetMoveFullInfo(Move move)
    {
        var capturedPositions = GetAllMoveCaptures(move).ToArray();
        var promotionPosition = TryGetQueenPromotionPosition(move.Path, move.PieceOnBoard.Piece.Color);
        return new MoveFullInfo(move, capturedPositions, promotionPosition);
    }
}