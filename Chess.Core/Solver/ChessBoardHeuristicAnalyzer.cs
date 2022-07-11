namespace Chess.Core.Solver;

public class ChessBoardHeuristicAnalyzer
{
    private readonly Random _random = new(264343821);

    private PieceColor _fromPerspective;
    private HeuristicAnalyzerConfig _config;

    private readonly PieceOnBoard[] _pieceBuffer = new PieceOnBoard[32];

    public ChessBoardHeuristicAnalyzer()
    {
        _config = new HeuristicAnalyzerConfig();
    }

    public ChessBoardHeuristicAnalyzer(HeuristicAnalyzerConfig config)
    {
        _config = config.Copy();
    }

    public void Configure(HeuristicAnalyzerConfig config)
    {
        _config = config.Copy();
    }

    public void Configure(Action<HeuristicAnalyzerConfig> configurator)
    {
        configurator(_config);
        _config = _config.Copy();
    }

    public double EvaluateBoard(ChessBoard board, PieceColor fromPerspective)
    {
        _fromPerspective = fromPerspective;

        var pieces = board.GetAllPieces()
            .Select(pos => new PieceOnBoard(board.GetPieceAt(pos), pos));

        var score = 0.0;

        score += EvaluateAlivePieces(pieces);
        score += GetGameEndScore(board.GetGameEndState());

        return score;
    }

    private double EvaluateAlivePieces(IEnumerable<PieceOnBoard> pieces)
    {
        var score = 0.0;

        foreach (var pieceOnBoard in pieces)
        {
            var pieceScore = pieceOnBoard.Piece.Type switch
            {
                PieceType.Pawn => _config.PawnAlive,
                PieceType.Bishop => _config.BishopAlive,
                PieceType.Knight => _config.KnightAlive,
                PieceType.Rook => _config.RookAlive,
                PieceType.Queen => _config.QueenAlive,
                _ => 0.0
            };

            score += MatchScore(pieceScore, pieceOnBoard.Piece.Color);
        }

        return score;
    }

    private double MatchScore(double score, PieceColor color)
    {
        return color == _fromPerspective ? score : -score;
    }

    public double GetGameEndScore(GameEndState gameEndState)
    {
        return gameEndState switch
        {
            GameEndState.Draw => HeuristicAnalyzerConfig.DrawScore,
            GameEndState.WhiteWin => _fromPerspective == PieceColor.White
                ? HeuristicAnalyzerConfig.VictoryScore
                : HeuristicAnalyzerConfig.DefeatScore,
            GameEndState.BlackWin => _fromPerspective == PieceColor.Black
                ? HeuristicAnalyzerConfig.VictoryScore
                : HeuristicAnalyzerConfig.DefeatScore,
            _ => 0
        };
    }
}