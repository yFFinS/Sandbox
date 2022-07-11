using Chess.Core;
using Microsoft.Xna.Framework;

namespace Chess.View;

public class MoveDrawable
{
    public static readonly Color DefaultPathColor = new(58, 158, 65);

    public MoveDrawable(Move move)
    {
        Move = move;
    }

    public readonly Move Move;
    public Color PathColor { get; set; } = DefaultPathColor;
    public int DrawOrder { get; set; }
    public int StaringIndex { get; set; } = -1;
}