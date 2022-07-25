namespace Chess.Core.Solver;

public struct SearchResults
{
    public int LmrSavedDepth { get; set; }
    public int Nodes { get; set; }
    public int MaxReachedDepth { get; set; }
    public int TranspositionTableHits { get; set; }
    public Move BestMove { get; set; }
    public int BestMoveScore { get; set; }
    public long TotalTime { get; set; }
}