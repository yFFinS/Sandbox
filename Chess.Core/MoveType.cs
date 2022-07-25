namespace Chess.Core;

public enum MoveType
{
    Quiet = 0,
    DoublePawn = 1,
    Capture = 13,
    EnPassant = 12,
    KingsideCastle = 10,
    QueensideCastle = 11,
    KnightPromotionQuiet = 4,
    BishopPromotionQuiet = 3,
    RookPromotionQuiet = 2,
    QueenPromotionQuiet = 5,
    KnightPromotionCapture = 8,
    BishopPromotionCapture = 7,
    RookPromotionCapture = 6,
    QueenPromotionCapture = 9
}