namespace Checkers;

public class MoveFullInfo
{
    public MoveFullInfo(Move move, IReadOnlyList<Position> capturedPositions, Position? promotionPosition)
    {
        Move = move;
        CapturedPositions = capturedPositions;
        PromotionPosition = promotionPosition;
        PromotionPathIndex =
            HasBeenPromoted ? Array.IndexOf(Move.Path.ToArray(), PromotionPosition!.Value) : int.MaxValue;
    }

    public readonly Move Move;
    public readonly IReadOnlyList<Position> CapturedPositions;
    public readonly Position? PromotionPosition;
    public readonly int PromotionPathIndex;
    public bool HasBeenPromoted => PromotionPosition.HasValue;
    public Piece Piece => Move.PieceOnBoard.Piece;
    public Position StartPosition => Move.PieceOnBoard.Position;
    public Position EndPosition => Move.Path[^1];
    public bool IsCapturing => CapturedPositions.Count > 0;
}