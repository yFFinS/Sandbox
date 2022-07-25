using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Chess.Core;

public struct ByColorIndexer<T>
{
    private T _white;
    private T _black;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly T Get(PieceColor color)
    {
        Debug.Assert(color is PieceColor.White or PieceColor.Black);
        return color == PieceColor.White ? _white : _black;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(PieceColor color, T value)
    {
        Debug.Assert(color is PieceColor.White or PieceColor.Black);
        if (color == PieceColor.White)
        {
            _white = value;
        }
        else
        {
            _black = value;
        }
    }
}