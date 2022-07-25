using System.Diagnostics;

namespace Chess.Core.Solver;

public class BoardAlphaBeta
{
    private readonly Board _board;
    private readonly BoardHeuristicAnalyzer _analyzer;
    private readonly double _maxTime;
    private readonly TranspositionTable _transpositionTable;
    private readonly MoveOrderer _moveOrderer;

    private SearchResults _searchResults;
    private readonly Stopwatch _timer = new();

    private bool IsTimeExpired => _timer.ElapsedMilliseconds >= _maxTime;

    private const int MaxPly = 50;
    private const int MaxKillerMoves = 3;
    private const int CheckmateScore = -10000;
    private const int StalemateScore = 0;
    private const int CheckmateThreshold = 9000;
    private const int MaxTtEntries = 4_000_000;
    private const int TtBucketSize = 3;

    public BoardAlphaBeta(Board board, BoardHeuristicAnalyzer analyzer, double maxTime)
    {
        _board = board;
        _analyzer = analyzer;
        _maxTime = maxTime;
        _transpositionTable = new TranspositionTable(MaxTtEntries, TtBucketSize);
        _moveOrderer = new MoveOrderer(MaxPly, MaxKillerMoves);
    }

    private int AlphaBeta(int depth, int alpha, int beta, List<Move> pv, bool isPvNode, SearchRefs refs)
    {
        if (IsTimeExpired)
        {
            return 0;
        }

        var startedInCheck = refs.Board.IsCheck();
        if (startedInCheck)
        {
            depth++;
        }

        if (depth <= 0)
        {
            return QuiescenceSearch(alpha, beta, pv, refs);
        }

        refs.Results.Nodes++;

        if (refs.Ply > refs.Results.MaxReachedDepth)
        {
            refs.Results.MaxReachedDepth = refs.Ply;
        }

        if (refs.Ply >= MaxPly)
        {
            return _analyzer.EvaluateBoard(_board);
        }

        var ttEntryType = EntryValueType.Alpha;

        var ttEntry = _transpositionTable.ProbeLockless(refs.Board.ZobristHash);
        if (ttEntry.HasValue && refs.Ply > 0)
        {
            var value = ttEntry.Value.Apply(depth, alpha, beta);
            if (value.HasValue)
            {
                refs.Results.TranspositionTableHits++;

                if (value > CheckmateThreshold)
                {
                    value -= refs.Ply;
                }

                if (value < -CheckmateThreshold)
                {
                    value += refs.Ply;
                }

                return value.Value;
            }
        }

        var doPvs = false;
        var bestScore = int.MinValue;
        var bestMove = Move.Empty;
        var newDepth = depth;

        Span<DetailedMove> tempMoves = stackalloc DetailedMove[256];
        var count = refs.Board.MoveGenerator.GetDetailedLegalMoves(tempMoves);
        var moves = tempMoves[..count];
        _moveOrderer.OrderMoves(refs.Ply, moves, ttEntry?.BestMove ?? Move.Empty);

        var searchedMoves = 0;
        foreach (var move in moves)
        {
            if (IsTimeExpired)
            {
                return 0;
            }

            refs.Ply++;
            refs.Board.MakeMove(move.Move);

            var lateMoveReduce = 0;

            if (!doPvs && !isPvNode && searchedMoves > 2 && depth > 2 && !startedInCheck && !refs.Board.IsCheck() &&
                move.Move.Type.IsReducible())
            {
                lateMoveReduce = 1 + Math.Min(searchedMoves / 6, 2);
            }

            newDepth -= lateMoveReduce;

            searchedMoves++;

            int score;
            var newPv = new List<Move>();
            if (doPvs)
            {
                score = -AlphaBeta(newDepth - 1, -alpha - 1, -alpha, newPv, true, refs);

                if (score > alpha && score < beta)
                {
                    score = -AlphaBeta(newDepth - 1, -beta, -alpha, newPv, true, refs);
                }
            }
            else
            {
                score = -AlphaBeta(newDepth - 1, -beta, -alpha, newPv, false, refs);
                if (score > alpha && lateMoveReduce > 0)
                {
                    score = -AlphaBeta(newDepth - 1 + lateMoveReduce, -beta, -alpha, newPv, false, refs);
                }
                else
                {
                    refs.Results.LmrSavedDepth += lateMoveReduce;
                }
            }

            refs.Board.RevertMove();
            refs.Ply--;

            if (score >= bestScore)
            {
                bestScore = score;
                bestMove = move.Move;
            }

            if (score >= beta)
            {
                _transpositionTable.Insert(new TableEntry
                {
                    Hash = refs.Board.ZobristHash,
                    Value = beta,
                    BestMove = bestMove,
                    SearchDepth = newDepth,
                    Type = EntryValueType.Beta
                });

                if (move.Move.Type.IsQuiet())
                {
                    _moveOrderer.StoreKillerMove(refs.Ply, move.Move);
                }

                return beta;
            }

            if (score > alpha)
            {
                doPvs = true;
                pv.Clear();
                pv.Add(move.Move);
                pv.AddRange(newPv);

                alpha = score;
                ttEntryType = EntryValueType.Exact;
            }
        }

        if (moves.Length == 0)
        {
            if (startedInCheck)
            {
                return CheckmateScore + refs.Ply;
            }

            return StalemateScore;
        }

        _transpositionTable.Insert(new TableEntry
        {
            Hash = refs.Board.ZobristHash,
            BestMove = bestMove,
            SearchDepth = newDepth,
            Value = alpha,
            Type = ttEntryType
        });

        return alpha;
    }

    private int QuiescenceSearch(int alpha, int beta, List<Move> pv, SearchRefs refs)
    {
        if (IsTimeExpired)
        {
            return 0;
        }

        refs.Results.Nodes++;

        var score = _analyzer.EvaluateBoard(refs.Board);

        if (refs.Ply > refs.Results.MaxReachedDepth)
        {
            refs.Results.MaxReachedDepth = refs.Ply;
        }

        if (refs.Ply >= MaxPly)
        {
            return score;
        }

        if (score > beta)
        {
            return beta;
        }

        if (score > alpha)
        {
            alpha = score;
        }

        Span<DetailedMove> tempMoves = stackalloc DetailedMove[256];
        var count = refs.Board.MoveGenerator.GetDetailedLegalMoves(tempMoves, true);
        var moves = tempMoves[..count];
        _moveOrderer.OrderMoves(refs.Ply, moves, Move.Empty);

        foreach (var move in moves)
        {
            if (IsTimeExpired)
            {
                return 0;
            }

            var newPv = new List<Move>();

            refs.Ply++;
            refs.Board.MakeMove(move.Move);
            score = -QuiescenceSearch(-beta, -alpha, newPv, refs);
            refs.Board.RevertMove();
            refs.Ply--;

            if (score >= beta)
            {
                return beta;
            }

            if (score > alpha)
            {
                alpha = score;
                pv.Clear();
                pv.Add(move.Move);
                pv.AddRange(newPv);
            }
        }

        return alpha;
    }

    public SearchResults Search()
    {
        var maxSearchedDepth = 0;

        var dummy = new object();

        var maxWorkers = 12;
        var workers = new SearchThread[maxWorkers];
        for (var i = 0; i < maxWorkers; i++)
        {
            workers[i] = new SearchThread(_board, (depth, refs) =>
            {
                var score = AlphaBeta(depth, int.MinValue + 1, int.MaxValue - 1, refs.PV, true, refs);

                if (IsTimeExpired)
                {
                    return;
                }

                lock (dummy)
                {
                    _searchResults.Nodes += refs.Results.Nodes;
                    _searchResults.LmrSavedDepth += refs.Results.LmrSavedDepth;
                    _searchResults.TranspositionTableHits += refs.Results.TranspositionTableHits;
                    _searchResults.TotalTime += _timer.ElapsedMilliseconds;
                    _searchResults.MaxReachedDepth =
                        Math.Max(_searchResults.MaxReachedDepth, refs.Results.MaxReachedDepth);

                    if (depth > maxSearchedDepth)
                    {
                        maxSearchedDepth = depth;
                        _searchResults.BestMove = refs.PV[0];
                        _searchResults.BestMoveScore = score;

                        Console.WriteLine($"Best move: {_searchResults.BestMove} - {_searchResults.BestMoveScore}");
                        Console.WriteLine($"PV: {string.Join(" ", refs.PV.Select(w => w.ToString()))}");
                        Console.WriteLine($"Nodes: {_searchResults.Nodes}");
                        Console.WriteLine($"Max depth: {_searchResults.MaxReachedDepth}");
                        Console.WriteLine($"TTHits: {_searchResults.TranspositionTableHits}");
                        Console.WriteLine($"LMRDepthSaved: {_searchResults.LmrSavedDepth}");
                        Console.WriteLine();
                    }
                }

                refs.IsReady = true;
            });
        }

        _timer.Restart();

        var id = 0;
        var depth = 1;
        var counter = 0;
        var ready = -maxWorkers;

        var wpd = new int[24];
        while (!IsTimeExpired)
        {
            var worker = workers[id];
            if (worker.IsReady)
            {
                if (worker.Refs.Results.BestMoveScore == -CheckmateScore - 1)
                {
                    break;
                }

                if (counter++ is 4 or 5 || counter >= 6 && counter % 2 == 0)
                {
                    depth++;
                }

                wpd[depth]++;
                worker.StartSearch(depth);
                ready++;
            }

            id = (id + 1) % workers.Length;
        }
        
        return _searchResults;
    }
}

internal class SearchRefs
{
    public bool IsReady { get; set; }
    public Board Board { get; init; }
    public List<Move> PV { get; init; }
    public int Ply;
    public SearchResults Results;
}

internal class SearchThread
{
    private readonly Action<int, SearchRefs> _search;
    public readonly SearchRefs Refs;
    public bool IsReady => Refs.IsReady;

    private Thread? _thread;

    public SearchThread(Board board, Action<int, SearchRefs> search)
    {
        _search = search;
        Refs = new SearchRefs
        {
            Board = new Board(board.ToFen()),
            PV = new List<Move>(),
            IsReady = true
        };
    }

    public void StartSearch(int depth)
    {
        Refs.IsReady = false;
        _thread = new Thread(() => _search(depth, Refs));
        _thread.Start();
    }
}