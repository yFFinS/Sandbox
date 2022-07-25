using System.Runtime.CompilerServices;

namespace Chess.Core;

public static class PieceColorExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PieceColor Opposite(this PieceColor color)
    {
        return color == PieceColor.Black ? PieceColor.White : PieceColor.Black;
    }
}