namespace Checkers.Core;

public class MoveInfo
{
    public MoveInfo(Move move, IReadOnlyList<Position> capturedPositions, Position? promotionPosition)
    {
        Move = move;
        CapturedPositions = capturedPositions;
        PromotionPosition = promotionPosition;
        PromotionPathIndex =
            HasPromoted ? Array.IndexOf(Move.Path.ToArray(), PromotionPosition!.Value) : int.MaxValue;
    }

    public readonly Move Move;
    public readonly IReadOnlyList<Position> CapturedPositions;
    public readonly Position? PromotionPosition;
    public readonly int PromotionPathIndex;
    public bool HasPromoted => PromotionPosition.HasValue;
    public Piece Piece => Move.PieceOnBoard.Piece;
    public Position StartPosition => Move.PieceOnBoard.Position;
    public Position EndPosition => Move.Path.Last();
    public bool IsCapturing => CapturedPositions.Count > 0;

    public Piece GetMovedPieceAtIndex(int pathIndex)
    {
        if (HasPromoted && pathIndex >= PromotionPathIndex)
        {
            return new Piece(PieceType.Queen, Piece.Color);
        }

        return Piece;
    }
}