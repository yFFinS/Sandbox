using System.Collections;
using Checkers.Core;
using Microsoft.Xna.Framework;
using Sandbox.Shared.UI;

namespace Checkers.View;

public class BoardDrawable
{
    private readonly Board _board;
    private readonly List<PieceDrawable> _pieces = new();
    private readonly List<CellDrawable> _cells = new();
    private readonly List<MoveDrawable> _moves = new();
    private readonly List<UiText> _cellIndices = new();

    public readonly UpdatedCellsController CellsController = new();

    private int _cellSize;

    private Rectangle _boardScreenRectangle;

    public BoardDrawable(Board board, Rectangle boardScreenRectangle)
    {
        _board = board;
        _boardScreenRectangle = boardScreenRectangle;

        var width = boardScreenRectangle.Width / _board.Size;
        var height = boardScreenRectangle.Height / _board.Size;
        _cellSize = Math.Min(width, height);

        InitializeFromBoard(_board);
    }

    public Position ToBoardPosition(Vector2 screenPosition)
    {
        return ToBoardPosition(new Point((int)screenPosition.X, (int)screenPosition.Y));
    }

    public Position ToBoardPosition(Point screenPosition)
    {
        var x = screenPosition.X / _cellSize - _boardScreenRectangle.Left;
        var y = screenPosition.Y / _cellSize - _boardScreenRectangle.Top;
        return new Position(x, y);
    }

    public void ClearMoves()
    {
        _moves.Clear();
    }

    public void InitializeFromBoard(Board board)
    {
        foreach (var cellIndex in _cellIndices)
        {
            cellIndex.Dispose();
        }

        _cellIndices.Clear();

        _cells.Clear();
        _pieces.Clear();

        for (var x = 0; x < board.Size; x++)
        {
            for (var y = 0; y < board.Size; y++)
            {
                var isBlack = (x + y) % 2 == 1;
                var position = new Position(x, y);
                var color = isBlack ? CellDrawable.BlackCellColor : CellDrawable.WhiteCellColor;
                _cells.Add(new CellDrawable
                {
                    DefaultColor = color,
                    Color = color,
                    BoardPosition = position,
                    Position = ToScreenPosition(position)
                });
                
                if (isBlack)
                {
                    _cellIndices.Add(new UiText
                    {
                        Bounds = GetCellRectangle(position),
                        Text = Board.GetCellName(position).ToUpper(),
                        FontScale = 0.35f,
                        TextColor = Color.White,
                        HorizontalTextAlignment = HorizontalAlignment.Right,
                        VerticalTextAlignment = VerticalAlignment.Top,
                        Padding = 2
                    });
                }
            }
        }

        foreach (var pieceOnBoard in board.GetAllPieces())
        {
            _pieces.Add(new PieceDrawable
            {
                Piece = pieceOnBoard.Piece,
                BoardPosition = pieceOnBoard.Position,
                Position = ToScreenPosition(pieceOnBoard.Position)
            });
        }
    }

    public void RemovePiece(PieceDrawable pieceDrawable)
    {
        _pieces.Remove(pieceDrawable);
    }

    public void AddPiece(PieceDrawable pieceDrawable)
    {
        _pieces.Add(pieceDrawable);
    }

    public CellDrawable? GetCellAt(Position position)
    {
        return _cells.FirstOrDefault(cell => cell.BoardPosition == position);
    }

    public PieceDrawable? GetPieceAt(Position position)
    {
        return _pieces.FirstOrDefault(piece => piece.BoardPosition == position);
    }

    public void AddMove(MoveDrawable moveDrawable)
    {
        _moves.Add(moveDrawable);
    }

    public void RemoveMove(MoveDrawable moveDrawable)
    {
        _moves.Remove(moveDrawable);
    }

    public Vector2 ToScreenPosition(Position position)
    {
        var screenPosition = new Vector2(position.X, position.Y) * _cellSize;
        screenPosition.X += _boardScreenRectangle.Left;
        screenPosition.Y += _boardScreenRectangle.Top;
        return screenPosition;
    }

    public IEnumerable<MoveDrawable> Moves => _moves;

    public IEnumerable<CellDrawable> Cells => _cells;
    public IEnumerable<PieceDrawable> Pieces => _pieces;

    public IEnumerable<UiText> CellIndices => _cellIndices;
    public int CellSize => _cellSize;
    public Rectangle ScreenRectangle => _boardScreenRectangle;

    public Rectangle GetCellRectangle(Vector2 screenTopLeft)
    {
        var x = (int)screenTopLeft.X;
        var y = (int)screenTopLeft.Y;
        return new Rectangle(x, y, _cellSize, _cellSize);
    }

    public Rectangle GetCellRectangle(Position cell)
    {
        return new Rectangle(cell.X * _cellSize, cell.Y * _cellSize, _cellSize, _cellSize);
    }

    public GameEndState GetGameEndState()
    {
        return _board.GetGameEndState();
    }

    public void RemovePieceAt(Position position)
    {
        var piece = GetPieceAt(position);
        if (piece is not null)
        {
            _pieces.Remove(piece);
        }
    }
}