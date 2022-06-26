using System.Diagnostics;

namespace Checkers.Core;

[DebuggerDisplay("{PieceOnBoard.Piece.Color} {PieceOnBoard.Position} -> {EndPosition}")]
public readonly struct Move
{
    public readonly PieceOnBoard PieceOnBoard;
    public readonly IReadOnlyList<Position> Path;
    public Position EndPosition => Path.Last();

    public Move(PieceOnBoard pieceOnBoard, IReadOnlyList<Position> path)
    {
        PieceOnBoard = pieceOnBoard;
        Path = path;
    }
}