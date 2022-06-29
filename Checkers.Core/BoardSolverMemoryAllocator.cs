using System.Runtime.CompilerServices;

namespace Checkers.Core;

public static class BoardSolverMemoryAllocator
{
    private static readonly Stack<Board> AvailableBoards = new();

    private static int _maxPreAllocatedBoards = 32 * 250_000;

    private static bool _isEnabled = true;

    public static void SetMaximumPreAllocatedBoards(int amount)
    {
        _maxPreAllocatedBoards = amount;
    }

    public static void DisablePreAllocation()
    {
        _isEnabled = false;
        lock (AvailableBoards)
        {
            AvailableBoards.Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void FreeBoard(Board source)
    {
        if (!_isEnabled)
        {
            return;
        }

        lock (AvailableBoards)
        {
            if (AvailableBoards.Count >= _maxPreAllocatedBoards)
            {
                return;
            }

            AvailableBoards.Push(source);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Board RequestBoardCopy(Board source)
    {
        if (!_isEnabled)
        {
            return source.Copy();
        }

        lock (AvailableBoards)
        {
            if (!AvailableBoards.TryPop(out var dummy))
            {
                return source.Copy();
            }

            source.CopyTo(dummy);
            return dummy;

        }
    }
}