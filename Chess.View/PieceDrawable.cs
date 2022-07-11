using Chess.Core;
using Microsoft.Xna.Framework;

namespace Chess.View;

public class PieceDrawable
{
    public static readonly Color WhitePieceColor = new(255, 247, 230);
    public static readonly Color BlackPieceColor = new(26, 24, 21);

    public int BoardPosition { get; set; }
    public Piece Piece { get; set; }
    public Vector2 ScreenPosition { get; set; }
    public int DrawOrder { get; set; }

    public void Promote(PieceType type)
    {
        Piece = new Piece(Piece.Color, type);
    }

    public void Demote()
    {
        Piece = new Piece(Piece.Color, PieceType.Pawn);
    }
}