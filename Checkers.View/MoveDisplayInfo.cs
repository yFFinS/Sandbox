using Checkers.Core;
using Microsoft.Xna.Framework;

namespace Checkers.View;

public struct MoveDisplayInfo
{
    public MoveFullInfo FullInfo { get; set; }
    public Color PathColor { get; set; }
    public Color PieceColor { get; set; }
    public int DrawLayer { get; set; }
}