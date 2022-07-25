namespace Chess.Core;

public class PieceSquareEvaluator
{
    private readonly ByPieceIndexer<int[]> _pieceSquareTables;

    private ByColorIndexer<int> _scores;
    public ReadOnlyColorIndexer<int> Scores => new(_scores);

    public void FeedRemoveAt(int square, Piece piece)
    {
        Feed(square, piece, -1);
    }

    public void FeedSetAt(int square, Piece piece)
    {
        Feed(square, piece, 1);
    }

    private void Feed(int square, Piece piece, int sign)
    {
        var table = _pieceSquareTables.Get(piece.Type);
        var color = piece.Color;
        var index = GetIndex(square, color);

        var score = _scores.Get(color);
        _scores.Set(color, score + sign * table[index]);
    }

    public void Reset()
    {
        _scores.Set(PieceColor.White, 0);
        _scores.Set(PieceColor.Black, 0);
    }

    private static int GetIndex(int square, PieceColor color)
    {
        return color == PieceColor.White ? square : 63 - square;
    }

    public PieceSquareEvaluator()
    {
        _pieceSquareTables = new ByPieceIndexer<int[]>();

        _pieceSquareTables.Set(PieceType.Pawn, new[]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            50, 50, 50, 50, 50, 50, 50, 50,
            10, 10, 20, 30, 30, 20, 10, 10,
            5, 5, 10, 27, 27, 10, 5, 5,
            0, 0, 0, 25, 25, 0, 0, 0,
            5, -5, -10, 0, 0, -10, -5, 5,
            5, 10, 10, -25, -25, 10, 10, 5,
            0, 0, 0, 0, 0, 0, 0, 0
        });

        _pieceSquareTables.Set(PieceType.Knight, new[]
        {
            -50, -40, -30, -30, -30, -30, -40, -50,
            -40, -20, 0, 0, 0, 0, -20, -40,
            -30, 0, 10, 15, 15, 10, 0, -30,
            -30, 5, 15, 20, 20, 15, 5, -30,
            -30, 0, 15, 20, 20, 15, 0, -30,
            -30, 5, 10, 15, 15, 10, 5, -30,
            -40, -20, 0, 5, 5, 0, -20, -40,
            -50, -40, -20, -30, -30, -20, -40, -50,
        });

        _pieceSquareTables.Set(PieceType.Bishop, new[]
        {
            -20, -10, -10, -10, -10, -10, -10, -20,
            -10, 0, 0, 0, 0, 0, 0, -10,
            -10, 0, 5, 10, 10, 5, 0, -10,
            -10, 5, 5, 10, 10, 5, 5, -10,
            -10, 0, 10, 10, 10, 10, 0, -10,
            -10, 10, 10, 10, 10, 10, 10, -10,
            -10, 5, 0, 0, 0, 0, 5, -10,
            -20, -10, -40, -10, -10, -40, -10, -20,
        });

        _pieceSquareTables.Set(PieceType.Rook, new[]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            5, 10, 10, 10, 10, 10, 10, 5,
            -5, 0, 0, 0, 0, 0, 0, -5,
            -5, 0, 0, 0, 0, 0, 0, -5,
            -5, 0, 0, 0, 0, 0, 0, -5,
            -5, 0, 0, 0, 0, 0, 0, -5,
            -5, 0, 0, 0, 0, 0, 0, -5,
            0, 0, 0, 5, 5, 0, 0, 0
        });

        _pieceSquareTables.Set(PieceType.King, new[]
        {
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -20, -30, -30, -40, -40, -30, -30, -20,
            -10, -20, -20, -20, -20, -20, -20, -10,
            20, 20, 0, 0, 0, 0, 20, 20,
            20, 30, 10, 0, 0, 10, 30, 20
        });

        _pieceSquareTables.Set(PieceType.Queen, new[]
        {
            -20, -10, -10, -5, -5, -10, -10, -20,
            -10, 0, 5, 0, 0, 0, 0, -10,
            -10, 0, 0, 0, 0, 0, 0, -10,
            -10, 0, 5, 5, 5, 5, 0, -10,
            -10, 5, 5, 5, 5, 5, 0, -10,
            0, 0, 5, 5, 5, 5, 0, -5,
            -5, 0, 5, 5, 5, 5, 0, -5,
            -20, -10, -10, -5, -5, -10, -10, -20
        });
    }
}