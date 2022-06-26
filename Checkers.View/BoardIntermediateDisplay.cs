using Checkers.Core;
using Microsoft.Xna.Framework;

namespace Checkers.View;

public class BoardIntermediateDisplay
{
    private readonly Board _board;
    private readonly BoardDrawer _drawer;

    private Position? _selectedCell;
    private Move? _moveCellsDisplayed;

    public BoardIntermediateDisplay(Board board, BoardDrawer drawer)
    {
        _board = board;
        _drawer = drawer;
    }

    public Position GetCellAt(Point screenPosition)
    {
        return _drawer.GetCellAt(screenPosition);
    }

    public void SetDisplayedAvailableMoves(IEnumerable<MoveDisplayInfo> moves)
    {
        _drawer.SetDisplayedMoves(moves);
    }

    public void SetPartialMovePreview(int partialPathIndex)
    {
        _drawer.SetPartialPathIndex(partialPathIndex);
    }

    public void SetSelectedCell(Position cell, bool canMove = false)
    {
        ResetSelectedCell();
        _selectedCell = cell;
        _drawer.SetCellColor(_selectedCell.Value, canMove ? new Color(93, 135, 54) : new Color(171, 55, 55));
    }

    public void ResetSelectedCell()
    {
        if (_selectedCell is null)
        {
            return;
        }

        _drawer.SetCellColor(_selectedCell!.Value, null);
        _selectedCell = null;

        if (_moveCellsDisplayed.HasValue)
        {
            SetMovePathCells(_moveCellsDisplayed.Value);
        }
    }

    public void SetMovePathCells(Move move)
    {
        ResetMovePathCells();
        
        _moveCellsDisplayed = move;
        foreach (var cell in move.Path)
        {
            _drawer.SetCellColor(cell, new Color(102, 81, 207));
        }
        _drawer.SetCellColor(move.PieceOnBoard.Position, new Color(57, 46, 153));
    }

    public void ResetMovePathCells()
    {
        if (_moveCellsDisplayed is null)
        {
            return;
        }
        
        foreach (var cell in _moveCellsDisplayed!.Value.Path
                     .Append(_moveCellsDisplayed.Value.PieceOnBoard.Position))
        {
            _drawer.SetCellColor(cell, null);
        }

        _moveCellsDisplayed = null;
    }

    public void ResetDisplayedMoves()
    {
        _drawer.SetDisplayedMoves(null);
    }

    public void ResetPartialMove()
    {
        _drawer.SetPartialPathIndex(-1);
    }
}