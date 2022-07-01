using Checkers.Core;
using Microsoft.Xna.Framework;

namespace Checkers.View;

public class PieceDrawable
{
    public static readonly Color WhitePieceColor = new(255, 247, 230);
    public static readonly Color BlackPieceColor = new(26, 24, 21);

    public Position BoardPosition { get; set; }
    public Piece Piece { get; set; }
    public Vector2 Position { get; set; }
    public bool PreviewCapture { get; set; }
    public int DrawOrder { get; set; }

    public void Promote()
    {
        Piece = new Piece(PieceType.Queen, Piece.Color);
    }

    public void Demote()
    {
        Piece = new Piece(PieceType.Pawn, Piece.Color);
    }
}