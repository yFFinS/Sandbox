namespace Chess.Core;

internal static class Rank
{
    public static readonly Bitboard R8 = 0xFFFF;
    public static readonly Bitboard R7 = R8 << 8;
    public static readonly Bitboard R6 = R8 << 16;
    public static readonly Bitboard R5 = R8 << 24;
    public static readonly Bitboard R4 = R8 << 32;
    public static readonly Bitboard R3 = R8 << 40;
    public static readonly Bitboard R2 = R8 << 48;
    public static readonly Bitboard R1 = R8 << 56;
}