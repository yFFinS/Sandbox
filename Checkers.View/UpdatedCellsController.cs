namespace Checkers.View;

public class UpdatedCellsController
{
    private readonly Dictionary<CellDrawable, CellMarker> _updatedMoveIndicatorCells = new();
    private readonly Dictionary<CellDrawable, CellMarker> _updatedPathCells = new();
    private readonly List<CellDrawable> _updatedCaptureCells = new();

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
            else if (_updatedCaptureCells.Contains(updatedCell))
            {
                updatedCell.Mark(CellMarker.MustCapture);
            }
            else
            {
                updatedCell.ResetColor();
            }
        }

        _updatedMoveIndicatorCells.Clear();
    }

    public void ResetUpdatedMustCaptureCells()
    {
        foreach (var captureCell in _updatedCaptureCells)
        {
            captureCell.ResetColor();
        }

        _updatedCaptureCells.Clear();
    }

    public void MarkCell(CellDrawable cellDrawable, CellMarker marker)
    {
        cellDrawable.Mark(marker);
        if (marker is CellMarker.MustCapture)
        {
            _updatedCaptureCells.Add(cellDrawable);
            return;
        }

        var destination = marker is CellMarker.MoveAvailable or CellMarker.NoMoveAvailable
            ? _updatedMoveIndicatorCells
            : _updatedPathCells;
        destination[cellDrawable] = marker;

        _updatedCaptureCells.Remove(cellDrawable);
    }
}