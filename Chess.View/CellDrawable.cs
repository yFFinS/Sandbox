using Chess.Core;
using Microsoft.Xna.Framework;

namespace Chess.View;

public class CellDrawable
{
    public static readonly Color WhiteCellColor = new(247, 184, 141);
    public static readonly Color BlackCellColor = new(84, 34, 9);

    private static readonly Color MoveAvailableColor = new(62, 122, 65);
    private static readonly Color NoMoveAvailableColor = new(145, 44, 44);
    private static readonly Color MovePathColor = new(78, 91, 130);
    private static readonly Color MoveStartColor = new(44, 62, 115);
    private static readonly Color MustCaptureColor = new(189, 58, 58);

    public int BoardPosition { get; init; }
    public Vector2 ScreenPosition { get; init; }
    public Color DefaultColor { get; init; }
    public Color Color { get; set; }

    public void ResetColor()
    {
        Color = DefaultColor;
    }

    public void Mark(CellMarker marker)
    {
        switch (marker)
        {
            case CellMarker.MoveAvailable:
                Color = MoveAvailableColor;
                break;
            case CellMarker.NoMoveAvailable:
                Color = NoMoveAvailableColor;
                break;
            case CellMarker.MoveStart:
                Color = MoveStartColor;
                break;
            case CellMarker.MovePath:
                Color = MovePathColor;
                break;
            case CellMarker.MustCapture:
                Color = MustCaptureColor;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(marker), marker, null);
        }
    }
}