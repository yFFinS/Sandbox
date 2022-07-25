using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Chess.Core;

[DebuggerDisplay("{DebugDisplayString,nq}")]
public struct Bitboard : IEquatable<Bitboard>
{
    public static readonly Bitboard Empty = new();
    public static readonly Bitboard Filled = new(ulong.MaxValue);

    internal string DebugDisplayString
    {
        get
        {
            var str = string.Empty;
            for (var pos = 0; pos < 64; pos++)
            {
                if (pos % 8 == 0)
                {
                    str += '\n';
                }

                str += TestAt(pos) ? '1' : '0';
            }

            return str;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard(ulong value)
    {
        Value = value;
    }

    public ulong Value { get; private set; }

    public static Bitboard FromSetBits(IEnumerable<int> bits)
    {
        return bits.Aggregate(0UL, (current, offset) => current | 1UL << offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetAt(int offset)
    {
        Value |= 1UL << offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ResetAt(int offset)
    {
        Value &= ~(1UL << offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Reset()
    {
        Value = 0;
    }

    public readonly IEnumerable<int> GetAllSetBits()
    {
        for (var i = 0; i < 64; ++i)
        {
            if ((Value & (1UL << i)) != 0)
            {
                yield return i;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static implicit operator Bitboard(ulong value)
    {
        return new Bitboard(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static implicit operator ulong(Bitboard bitmap)
    {
        return bitmap.Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TestAt(int offset)
    {
        return (Value & (1UL << offset)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Bitboard operator |(Bitboard lhs, Bitboard rhs)
    {
        return lhs.Value | rhs.Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Bitboard operator &(Bitboard lhs, Bitboard rhs)
    {
        return lhs.Value & rhs.Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Bitboard operator ^(Bitboard lhs, Bitboard rhs)
    {
        return lhs.Value ^ rhs.Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Bitboard operator ~(Bitboard bitboard)
    {
        return ~bitboard.Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Bitboard operator >> (Bitboard bitboard, int offset)
    {
        return bitboard.Value >> offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Bitboard operator <<(Bitboard bitboard, int offset)
    {
        return bitboard.Value << offset;
    }

    private static readonly int[] DeBruijnIndex64 =
    {
        0, 1, 17, 2, 18, 50, 3, 57,
        47, 19, 22, 51, 29, 4, 33, 58,
        15, 48, 20, 27, 25, 23, 52, 41,
        54, 30, 38, 5, 43, 34, 59, 8,
        63, 16, 49, 56, 46, 21, 28, 32,
        14, 26, 24, 40, 53, 37, 42, 7,
        62, 55, 45, 31, 13, 39, 36, 6,
        61, 44, 12, 35, 60, 11, 10, 9,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static int BitScanForward(ulong value)
    {
        const ulong deBruijn64 = 0x37E84A99DAE458FUL;

        Debug.Assert(value != 0);
        return DeBruijnIndex64[((ulong)((long)value & -(long)value) * deBruijn64) >> 58];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int BitScanForward()
    {
        return BitScanForward(Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int BitScanForwardAll(Span<int> destination, int start = 0)
    {
        if (Value == 0)
        {
            return start;
        }

        var value = Value;

        var index = start;

        while (value != 0)
        {
            var bitPosition = BitScanForward(value);
            destination[index++] = bitPosition;
            value &= ~(1UL << bitPosition);
        }

        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int PopCount()
    {
        return BitOperations.PopCount(Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Equals(Bitboard other)
    {
        return Value == other.Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        return obj is Bitboard other && Equals(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Bitboard left, Bitboard right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Bitboard left, Bitboard right)
    {
        return !left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Contains(Bitboard subset)
    {
        return (this & subset) == subset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Intersects(Bitboard bitboard)
    {
        return (this & bitboard) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Bitboard WithSetBit(int bit)
    {
        var bitboard = Bitboard.Empty;
        bitboard.SetAt(bit);
        return bitboard;
    }
}