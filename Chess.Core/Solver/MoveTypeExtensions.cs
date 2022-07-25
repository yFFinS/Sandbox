namespace Chess.Core.Solver;

public static class MoveTypeExtensions
{
    public static bool IsCapture(this MoveType moveType)
    {
        return moveType is MoveType.Capture or MoveType.EnPassant or MoveType.BishopPromotionCapture
            or MoveType.KnightPromotionCapture or MoveType.RookPromotionCapture or MoveType.QueenPromotionCapture;
    }

    public static bool IsQuiet(this MoveType moveType)
    {
        return !IsCapture(moveType);
    }

    public static bool IsReducible(this MoveType moveType)
    {
        return IsCapture(moveType) ||
               moveType is MoveType.DoublePawn
                   or MoveType.KingsideCastle
                   or MoveType.QueensideCastle
                   or MoveType.BishopPromotionQuiet
                   or MoveType.KnightPromotionQuiet
                   or MoveType.RookPromotionQuiet
                   or MoveType.QueenPromotionQuiet;
    }
}