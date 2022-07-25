using System.Diagnostics;

namespace Chess.Core.Solver;

public class MoveOrderer
{
    private readonly int _maxPly;
    private readonly int _maxKillerMoves;
    private readonly Move[][] _killerMoves;
    private readonly int[] _storedKillerMoves;

    public MoveOrderer(int maxPly, int maxKillerMoves)
    {
        _maxPly = maxPly;
        _maxKillerMoves = maxKillerMoves;

        Debug.Assert(maxPly > 0);
        Debug.Assert(maxKillerMoves >= 0);

        _storedKillerMoves = new int[maxPly + 1];
        _killerMoves = new Move[maxPly + 1][];
        for (var i = 0; i <= maxPly; i++)
        {
            _killerMoves[i] = new Move[maxKillerMoves];
        }
    }

    public void StoreKillerMove(int ply, Move move)
    {
        Debug.Assert(ply >= 0 && ply <= _maxPly);
        lock(_killerMoves)
        {
            var storedCount = _storedKillerMoves[ply];

            storedCount = (storedCount + 1) % _maxKillerMoves;
            _storedKillerMoves[ply] = storedCount;
            _killerMoves[ply][storedCount] = move;
        }
    }

    private struct ScoredMove : IComparable<ScoredMove>
    {
        public DetailedMove DetailedMove { get; init; }
        public int Score { get; set; }

        public int CompareTo(ScoredMove other)
        {
            return -Score.CompareTo(other.Score);
        }
    }

    public void OrderMoves(int ply, Span<DetailedMove> moves, Move ttMove)
    {
        Span<ScoredMove> scoredMoves = stackalloc ScoredMove[moves.Length];
        for (var i = 0; i < moves.Length; i++)
        {
            scoredMoves[i] = new ScoredMove
            {
                DetailedMove = moves[i],
                Score = 0
            };
        }

        ScoreMoves(ply, scoredMoves, ttMove);
        scoredMoves.Sort();

        for (var i = 0; i < moves.Length; i++)
        {
            moves[i] = scoredMoves[i].DetailedMove;
        }
    }

    private static readonly int[][] MvvLva =
    {
        new[] { 15, 14, 13, 12, 11, 10 },
        new[] { 25, 24, 23, 22, 21, 20 },
        new[] { 35, 34, 33, 32, 31, 30 },
        new[] { 45, 44, 43, 42, 41, 40 },
        new[] { 55, 54, 53, 52, 51, 50 },
    };

    private const int MvvLvaOffset = int.MaxValue - 256;
    private const int KillerMoveOffset = 1_000_000;
    private const int TtMoveValue = 100;

    private void ScoreMoves(int ply, Span<ScoredMove> moves, Move ttMove)
    {
        for (var i = 0; i < moves.Length; i++)
        {
            var moveValue = 0;

            var scoredMove = moves[i];
            var move = scoredMove.DetailedMove;
            if (move.Move == ttMove)
            {
                moveValue = MvvLvaOffset + TtMoveValue;
            }
            else if (move.Move.Type.IsCapture())
            {
                var attacked = (int)move.MovedPiece.Type;
                var victim = (int)move.CapturedPiece.Type;
                moveValue = MvvLvaOffset + MvvLva[victim][attacked];
            }
            else
            {
                foreach (var killerMove in _killerMoves[ply])
                {
                    if (killerMove != move.Move)
                    {
                        continue;
                    }

                    moveValue = KillerMoveOffset - i;
                    break;
                }
            }

            moveValue += (int)scoredMove.DetailedMove.Move.Type;
            moves[i].Score = moveValue;
        }
    }
}