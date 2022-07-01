namespace Checkers.View;

public class UpdatedCellsController
{
    private readonly Dictionary<CellDrawable, CellMarker> _updatedMoveIndicatorCells = new();
    private readonly Dictionary<CellDrawable, CellMarker> _updatedPathCells = new();

    public void ResetUpdatedPathCells()
    {
        foreach (var updatedCell in _updatedPathCells.Keys)
        {
            if (_updatedMoveIndicatorCells.TryGetValue(updatedCell, out var marker))
            {
                updatedCell.Mark(marker);
            }
            else
            {
                updatedCell.ResetColor();
            }
        }

        _updatedPathCells.Clear();
    }

    public void ResetUpdatedMoveIndicatorCells()
    {
        foreach (var updatedCell in _updatedMoveIndicatorCells.Keys)
        {
            if (_updatedPathCells.TryGetValue(updatedCell, out var marker))
            {
                updatedCell.Mark(marker);
            }
            else
            {
                updatedCell.ResetColor();
            }
        }

        _updatedMoveIndicatorCells.Clear();
    }

    public void MarkCell(CellDrawable cellDrawable, CellMarker marker)
    {
        cellDrawable.Mark(marker);
        var destination = marker is CellMarker.MoveAvailable or CellMarker.NoMoveAvailable
            ? _updatedMoveIndicatorCells
            : _updatedPathCells;
        destination[cellDrawable] = marker;
    }
}