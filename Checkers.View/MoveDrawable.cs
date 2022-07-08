using Checkers.Core;
using Microsoft.Xna.Framework;

namespace Checkers.View;

public class MoveDrawable
{
    public static readonly Color DefaultPathColor = new(58, 158, 65);

    public MoveDrawable(MoveInfo moveInfo)
    {
        MoveInfo = moveInfo;
    }

    public readonly MoveInfo MoveInfo;
    public Color PathColor { get; set; } = DefaultPathColor;
    public int DrawOrder { get; set; }
    public int StaringIndex { get; set; } = -1;
}