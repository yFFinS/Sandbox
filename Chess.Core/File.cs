namespace Chess.Core;

internal static class File
{
    public static readonly Bitboard A = 0x1010101010101010;
    public static readonly Bitboard B = A << 1;
    public static readonly Bitboard C = A << 2;
    public static readonly Bitboard D = A << 3;
    public static readonly Bitboard E = A << 4;
    public static readonly Bitboard F = A << 5;
    public static readonly Bitboard G = A << 6;
    public static readonly Bitboard H = A << 7;
}