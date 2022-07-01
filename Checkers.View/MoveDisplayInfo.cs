using Checkers.Core;
using Microsoft.Xna.Framework;

namespace Checkers.View;

public struct MoveDisplayInfo
{
    public MoveInfo Info { get; init; }
    public Color PathColor { get; init; }
    public Color PieceColor { get; set; }
    public int DrawLayer { get; init; }
}