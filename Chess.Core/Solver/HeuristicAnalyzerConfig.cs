namespace Chess.Core.Solver;

public class HeuristicAnalyzerConfig
{
    public int CheckmateScore { get; init; } = 10000;
    public int DrawScore { get; init; } = 0;

    public HeuristicAnalyzerConfig Copy()
    {
        return (HeuristicAnalyzerConfig)MemberwiseClone();
    }

    public HeuristicAnalyzerConfig()
    {
        var pieceAliveScore = new ByPieceIndexer<int>();
        pieceAliveScore.Set(PieceType.Pawn, 100);
        pieceAliveScore.Set(PieceType.Knight, 300);
        pieceAliveScore.Set(PieceType.Bishop, 320);
        pieceAliveScore.Set(PieceType.Rook, 450);
        pieceAliveScore.Set(PieceType.Queen, 950);

        PieceAliveScore = pieceAliveScore;

        var piecePinnedScore = new ByPieceIndexer<int>();
        piecePinnedScore.Set(PieceType.Pawn, 10);
        piecePinnedScore.Set(PieceType.Knight, 50);
        piecePinnedScore.Set(PieceType.Bishop, 35);
        piecePinnedScore.Set(PieceType.Rook, 60);
        piecePinnedScore.Set(PieceType.Queen, 100);

        PiecePinnedScore = piecePinnedScore;

        var pieceAttackedScore = new ByPieceIndexer<int>();
        pieceAttackedScore.Set(PieceType.Pawn, 3);
        pieceAttackedScore.Set(PieceType.Knight, 10);
        pieceAttackedScore.Set(PieceType.Bishop, 10);
        pieceAttackedScore.Set(PieceType.Rook, 20);
        pieceAttackedScore.Set(PieceType.Queen, 50);

        PieceAttackedScore = pieceAttackedScore;
    }

    public int CheckScore { get; init; } = -20;
    public int DoubleCheckScore { get; init; } = -50;

    public ReadOnlyPieceIndexer<int> PieceAliveScore { get; init; }
    public ReadOnlyPieceIndexer<int> PiecePinnedScore { get; init; }
    public ReadOnlyPieceIndexer<int> PieceAttackedScore { get; init; }
}