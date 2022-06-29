namespace Checkers.Core;

public class HeuristicAnalyzerConfig
{
    public const int VictoryScore = int.MaxValue;
    public const int DefeatScore = -VictoryScore;
    public const int DrawScore = 0;
    public const int MaxRandomScoreExclusive = 10;

    public const int PerTurnBeforeStalemateScore = 100;
    public const int PerTurnBeforeMaxTurnsScore = 20;

    public int PawnScorePerCellFromBorder { get; set; } = -5;
    public int QueenScorePerCellFromBorder { get; set; } = 5;
    public int PromotionCellFreeScore { get; set; } = 20;
    public int PawnMovableScore { get; set; } = 60;
    public int QueenMovableScore { get; set; } = 25;
    public int PawnAtBorderScore { get; set; } = 50;
    public int QueenAtBorderScore { get; set; } = 20;
    public int PawnAliveScore { get; set; } = 350;
    public int QueenAliveScore { get; set; } = 1000;
    public int PieceCountDifferenceScore { get; set; } = 50;
    public int DefenderPieceScore { get; set; } = 15;
    public int AttackerPawnScore { get; set; } = 20;
    public int CentralPawnScore { get; set; } = 30;
    public int MainDiagonalPawnScore { get; set; } = 40;
    public int MainDiagonalQueenScore { get; set; } = 70;
    public int DoubleDiagonalPawnScore { get; set; } = 30;
    public int DoubleDiagonalQueenScore { get; set; } = 60;
    public int LonerPawnScore { get; set; } = 50;
    public int LonerQueenScore { get; set; } = 100;
    public int HoleScore { get; set; } = 170;
    public int TriangleScore { get; set; } = 120;
    public int OreoScore { get; set; } = 90;
    public int BridgeScore { get; set; } = 100;
    public int DogScore { get; set; } = 250;
    public int CorneredQueen { get; set; } = 500;
    public int CorneredPawn { get; set; } = 100;

    public HeuristicAnalyzerConfig Copy()
    {
        return (HeuristicAnalyzerConfig)MemberwiseClone();
    }
}