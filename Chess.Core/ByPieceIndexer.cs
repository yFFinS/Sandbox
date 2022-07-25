using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Chess.Core;

public struct ByPieceIndexer<T>
{
    private T _pawns;
    private T _kings;
    private T _knights;
    private T _bishops;
    private T _rooks;
    private T _queens;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly T Get(PieceType pieceType)
    {
        return pieceType switch
        {
            PieceType.Pawn => _pawns,
            PieceType.Knight => _knights,
            PieceType.Bishop => _bishops,
            PieceType.Rook => _rooks,
            PieceType.King => _kings,
            PieceType.Queen => _queens,
            _ => throw new ArgumentOutOfRangeException(nameof(pieceType), pieceType, null)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(PieceType pieceType, T value)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                _pawns = value;
                break;
            case PieceType.Knight:
                _knights = value;
                break;
                ;
            case PieceType.Bishop:
                _bishops = value;
                break;
            case PieceType.Rook:
                _rooks = value;
                break;
            case PieceType.King:
                _kings = value;
                break;
            case PieceType.Queen:
                _queens = value;
                break;
            default:
                Debug.Assert(false);
                throw new ArgumentOutOfRangeException(nameof(pieceType), pieceType, null);
        }
    }
}