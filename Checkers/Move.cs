namespace Checkers;

public class Move
{
    public readonly PieceOnBoard PieceOnBoard;
    public readonly IReadOnlyList<Position> Path;

    public Move(PieceOnBoard pieceOnBoard, IReadOnlyList<Position> path)
    {
        PieceOnBoard = pieceOnBoard;
        Path = path;
    }
}