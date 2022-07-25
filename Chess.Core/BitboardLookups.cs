using System.Diagnostics;

namespace Chess.Core;

internal static class BitboardLookups
{
    public static readonly Bitboard[][] PawnAttacks = new Bitboard[2][];
    public static readonly Bitboard[][] PawnPushes = new Bitboard[2][];
    public static readonly Bitboard[][] PawnAttackedSquares = new Bitboard[2][];

    public static readonly Bitboard[] KnightMoves = new Bitboard[64];
    public static readonly Bitboard[] KnightAttackedSquares = new Bitboard[64];

    public static readonly Bitboard[] KingAttackedSquares = new Bitboard[64];
    public static readonly Bitboard[] KingMoves = new Bitboard[64];

    public static readonly Bitboard[][] InBetween = new Bitboard[64][];

    public static readonly Bitboard[] Files = new Bitboard[8];
    public static readonly Bitboard[] Ranks = new Bitboard[8];
    public static readonly Bitboard[] Diagonals = new Bitboard[64];
    public static readonly Bitboard[] AntiDiagonals = new Bitboard[64];

    public static readonly Dictionary<Bitboard, Bitboard>[] DiagonalMovesByOccupancy =
        new Dictionary<Bitboard, Bitboard>[64];

    public static readonly Dictionary<Bitboard, Bitboard>[] AntiDiagonalMovesByOccupancy =
        new Dictionary<Bitboard, Bitboard>[64];

    public static readonly Dictionary<Bitboard, Bitboard>[] FileMovesByOccupancy =
        new Dictionary<Bitboard, Bitboard>[64];

    public static readonly Dictionary<Bitboard, Bitboard>[] RankMovesByOccupancy =
        new Dictionary<Bitboard, Bitboard>[64];

    private static bool InBounds(int x, int y)
    {
        return x is >= 0 and < 8 && y is >= 0 and < 8;
    }

    static BitboardLookups()
    {
        PreComputeFilesAndRanks();
        PreComputeDiagonalsAndAntiDiagonals();
        PreComputeInBetween();

        PreComputePawnMoves();
        PreComputeKnightMoves();
        PreComputeKingMoves();

        PreComputeDiagonalAndAntiDiagonalMovesByOccupancy();
        PreComputeFileAndRankMovesByOccupancy();
    }

    private static void PreComputeFileAndRankMovesByOccupancy()
    {
        for (var pos = 0; pos < 64; pos++)
        {
            FileMovesByOccupancy[pos] = new Dictionary<Bitboard, Bitboard>();
            PreComputeSliderOccupancyMap(FileMovesByOccupancy[pos], pos, new[] { (0, -1), (0, 1) });
        }

        for (var pos = 0; pos < 64; pos++)
        {
            RankMovesByOccupancy[pos] = new Dictionary<Bitboard, Bitboard>();
            PreComputeSliderOccupancyMap(RankMovesByOccupancy[pos], pos, new[] { (-1, 0), (1, 0) });
        }
    }

    private static void PreComputeDiagonalAndAntiDiagonalMovesByOccupancy()
    {
        for (var pos = 0; pos < 64; pos++)
        {
            DiagonalMovesByOccupancy[pos] = new Dictionary<Bitboard, Bitboard>();
            PreComputeSliderOccupancyMap(DiagonalMovesByOccupancy[pos], pos, new[] { (-1, -1), (1, 1) });
        }

        for (var pos = 0; pos < 64; pos++)
        {
            AntiDiagonalMovesByOccupancy[pos] = new Dictionary<Bitboard, Bitboard>();
            PreComputeSliderOccupancyMap(AntiDiagonalMovesByOccupancy[pos], pos, new[] { (1, -1), (-1, 1) });
        }
    }

    private static void PreComputeFilesAndRanks()
    {
        for (var file = 0; file < 8; file++)
        {
            Files[file] = 0x0101010101010101UL << file;
        }

        for (var rank = 0; rank < 8; rank++)
        {
            Ranks[rank] = 0xFFUL << (rank * 8);
        }
    }

    private static void PreComputeDiagonalsAndAntiDiagonals()
    {
        for (var pos = 0; pos < 64; pos++)
        {
            var (x, y) = ToCoord(pos);
            var diagonal = new Bitboard();
            for (var dist = -8; dist <= 8; dist++)
            {
                var nx = x + dist;
                var ny = y + dist;
                if (InBounds(nx, ny))
                {
                    diagonal.SetAt(ToPosition(nx, ny));
                }
            }

            Diagonals[pos] = diagonal;
        }

        for (var pos = 0; pos < 64; pos++)
        {
            var (x, y) = ToCoord(pos);
            var antiDiagonal = new Bitboard();
            for (var dist = -8; dist <= 8; dist++)
            {
                var nx = x + dist;
                var ny = y - dist;
                if (InBounds(nx, ny))
                {
                    antiDiagonal.SetAt(ToPosition(nx, ny));
                }
            }

            AntiDiagonals[pos] = antiDiagonal;
        }
    }

    public static (int, int) ToCoord(int position)
    {
        return (position % 8, position / 8);
    }

    private static int ToPosition(int x, int y)
    {
        return x + y * 8;
    }

    private static void PreComputeSliderOccupancyMap(IDictionary<Bitboard, Bitboard> destination, int position,
        (int, int)[] directions)
    {
        List<int> GenerateMovesWithOccupancy(Bitboard occupancy)
        {
            var moves = new List<int>();
            var (x, y) = ToCoord(position);

            foreach (var (dx, dy) in directions)
            {
                for (var dist = 1; dist < 8; dist++)
                {
                    var nx = x + dx * dist;
                    var ny = y + dy * dist;
                    if (!InBounds(nx, ny))
                    {
                        break;
                    }

                    var endPos = ToPosition(nx, ny);
                    moves.Add(endPos);
                    if (occupancy.TestAt(endPos))
                    {
                        break;
                    }
                }
            }

            moves.Remove(position);
            return moves;
        }

        var allMoves = GenerateMovesWithOccupancy(0);
        allMoves.Add(position);

        void ComputeFromIndex(int index, Bitboard occupancy)
        {
            while (true)
            {
                if (index == allMoves.Count)
                {
                    Debug.Assert(!destination.ContainsKey(occupancy));
                    destination[occupancy] = Bitboard.FromSetBits(GenerateMovesWithOccupancy(occupancy));
                    return;
                }

                ComputeFromIndex(index + 1, occupancy);
                occupancy.SetAt(allMoves[index++]);
            }
        }

        ComputeFromIndex(0, 0);
    }

    private static void PreComputeInBetween()
    {
        // https://www.chessprogramming.org/Square_Attacked_By

        const ulong m1 = ~0UL;
        const ulong a2a7 = 0x0001010101010100UL;
        const ulong b2g7 = 0x0040201008040200UL;
        const ulong h1b7 = 0x0002040810204080UL;

        for (var from = 0; from < 64; from++)
        {
            InBetween[from] = new Bitboard[64];
            for (var to = 0; to < 64; to++)
            {
                var between = (m1 << from) ^ (m1 << to);
                var file = (ulong)((to & 7) - (from & 7));
                var rank = (ulong)((to | 7) - from) >> 3;
                var line = ((file & 7) - 1) & a2a7;
                line += 2 * (((rank & 7) - 1) >> 58);
                line += (((rank - file) & 15) - 1) & b2g7;
                line += (((rank + file) & 15) - 1) & h1b7;
                line *= (ulong)((long)between & -(long)between);
                //line *= between ^ (between - 1);
                InBetween[from][to] = line & between;
            }
        }
    }

    private static void PreComputeSliderMoves(Dictionary<Bitboard, Bitboard>[] destination, (int, int)[] directions)
    {
        for (var pos = 0; pos < 64; pos++)
        {
            PreComputeSliderOccupancyMap(destination[pos], pos, directions);
        }
    }

    private static void PreComputeKingMoves()
    {
        for (var pos = 0; pos < 64; pos++)
        {
            var (x, y) = ToCoord(pos);
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0 || !InBounds(x + dx, y + dy))
                    {
                        continue;
                    }

                    var endPos = ToPosition(x + dx, y + dy);
                    KingMoves[pos].SetAt(endPos);
                    KingAttackedSquares[endPos].SetAt(pos);
                }
            }
        }
    }

    private static void PreComputeKnightMoves()
    {
        for (var pos = 0; pos < 64; pos++)
        {
            var (x, y) = ToCoord(pos);
            foreach (var (dx, dy) in new[] { (-2, -1), (-2, 1), (2, -1), (2, 1), (1, 2), (1, -2), (-1, 2), (-1, -2) })
            {
                if (!InBounds(x + dx, y + dy))
                {
                    continue;
                }

                var endPos = ToPosition(x + dx, y + dy);
                KnightMoves[pos].SetAt(endPos);
                KnightAttackedSquares[endPos].SetAt(pos);
            }
        }
    }

    private static void PreComputePawnMoves()
    {
        foreach (var color in new[] { PieceColor.Black, PieceColor.White })
        {
            var destPushes = PawnPushes[(int)color] = new Bitboard[64];
            var destAttacks = PawnAttacks[(int)color] = new Bitboard[64];
            var destAttacked = PawnAttackedSquares[(int)color] = new Bitboard[64];

            var dy = color == PieceColor.Black ? 1 : -1;
            for (var pos = 0; pos < 64; pos++)
            {
                var (x, y) = ToCoord(pos);
                var offsets = color == PieceColor.Black
                    ? new[] { (1, 9), (-1, 7), (0, 8) }
                    : new[] { (-1, -9), (1, -7), (0, -8) };
                foreach (var (dx, offset) in offsets)
                {
                    var (nx, ny) = (x + dx, y + dy);
                    if (!InBounds(nx, ny))
                    {
                        continue;
                    }

                    if (Math.Abs(offset) != 8)
                    {
                        destAttacked[pos + offset].SetAt(pos);
                        destAttacks[pos].SetAt(pos + offset);
                    }
                    else
                    {
                        destPushes[pos].SetAt(pos + offset);
                    }
                }

                if (y == 1 && color == PieceColor.Black || y == 6 && color == PieceColor.White)
                {
                    destPushes[pos].SetAt(pos + 16 * dy);
                }
            }
        }
    }

    public static Bitboard GetFileMoves(int square, Bitboard occupancy)
    {
        var fileOccupancy = Files[square % 8] & occupancy;
        return FileMovesByOccupancy[square][fileOccupancy];
    }

    public static Bitboard GetRankMoves(int square, Bitboard occupancy)
    {
        var rankOccupancy = Ranks[square / 8] & occupancy;
        return RankMovesByOccupancy[square][rankOccupancy];
    }

    public static Bitboard GetDiagonalMoves(int square, Bitboard occupancy)
    {
        var diagMask = Diagonals[square];
        var diagOcc = diagMask & occupancy;
        return DiagonalMovesByOccupancy[square][diagOcc];
    }

    public static Bitboard GetAntiDiagonalMoves(int square, Bitboard occupancy)
    {
        var diagMask = AntiDiagonals[square];
        var diagOcc = diagMask & occupancy;
        return AntiDiagonalMovesByOccupancy[square][diagOcc];
    }
}