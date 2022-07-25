using Chess.Core;
using Microsoft.Xna.Framework;
using Sandbox.Shared.UI;

namespace Chess.View;

public class BoardDrawable
{
    public readonly Board Board;
    private readonly List<PieceDrawable> _pieces = new();
    private readonly List<CellDrawable> _cells = new();
    private readonly List<UiText> _cellIndices = new();

    public readonly UpdatedCellsController CellsController = new();

    private int _cellSize;

    private Rectangle _boardScreenRectangle;

    public BoardDrawable(Board board, Rectangle boardScreenRectangle)
    {
        Board = board;
        _boardScreenRectangle = boardScreenRectangle;

        var width = boardScreenRectangle.Width / 8;
        var height = boardScreenRectangle.Height / 8;
        _cellSize = Math.Min(width, height);

        InitializeFromBoard(Board);
    }

    public int CheckPosition { get; set; } = -1;

    public int ToBoardPosition(Vector2 screenPosition)
    {
        return ToBoardPosition(new Point((int)screenPosition.X, (int)screenPosition.Y));
    }

    public int ToBoardPosition(Point screenPosition)
    {
        var x = screenPosition.X / _cellSize - _boardScreenRectangle.Left;
        var y = screenPosition.Y / _cellSize - _boardScreenRectangle.Top;
        return x + y * 8;
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

        for (var x = 0; x < 8; x++)
        {
            for (var y = 0; y < 8; y++)
            {
                var position = y * 8 + x;
                var isBlack = (x + y) % 2 == 1;
                var color = isBlack ? CellDrawable.BlackCellColor : CellDrawable.WhiteCellColor;
                _cells.Add(new CellDrawable
                {
                    DefaultColor = color,
                    Color = color,
                    BoardPosition = position,
                    ScreenPosition = ToScreenPosition(position)
                });

                var textColor = isBlack ? Color.White : Color.Black;
                _cellIndices.Add(new UiText
                {
                    Bounds = GetCellRectangle(position),
                    Text = Board.GetSquareName(position).ToUpper(),
                    FontScale = 0.35f,
                    TextColor = textColor,
                    HorizontalTextAlignment = HorizontalAlignment.Right,
                    VerticalTextAlignment = VerticalAlignment.Top,
                    Padding = 2
                });
            }
        }

        foreach (var position in board.GetAllPieces())
        {
            var piece = board.GetPieceAt(position);
            _pieces.Add(new PieceDrawable
            {
                Piece = piece,
                BoardPosition = position,
                ScreenPosition = ToScreenPosition(position)
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

    public CellDrawable? GetCellAt(int position)
    {
        return _cells.FirstOrDefault(cell => cell.BoardPosition == position);
    }

    public PieceDrawable? GetPieceAt(int position)
    {
        return _pieces.FirstOrDefault(piece => piece.BoardPosition == position);
    }

    public Vector2 ToScreenPosition(int position)
    {
        // ReSharper disable once PossibleLossOfFraction
        var screenPosition = new Vector2(position % 8, position / 8) * _cellSize;
        screenPosition.X += _boardScreenRectangle.Left;
        screenPosition.Y += _boardScreenRectangle.Top;
        return screenPosition;
    }

    public IEnumerable<CellDrawable> Cells => _cells;
    public IEnumerable<PieceDrawable> Pieces => _pieces;

    public IEnumerable<UiText> CellIndices => _cellIndices;
    public int CellSize => _cellSize;
    public Rectangle ScreenRectangle => _boardScreenRectangle;
    public IEnumerable<MoveDrawable> Moves => _moves;

    public Rectangle GetCellRectangle(Vector2 screenTopLeft)
    {
        var x = (int)screenTopLeft.X;
        var y = (int)screenTopLeft.Y;
        return new Rectangle(x, y, _cellSize, _cellSize);
    }

    public Rectangle GetCellRectangle(int position)
    {
        return new Rectangle((position % 8) * _cellSize, (position / 8) * _cellSize, _cellSize, _cellSize);
    }

    public GameEndState GetGameEndState()
    {
        return Board.GetGameEndState();
    }

    public void RemovePieceAt(int position)
    {
        var piece = GetPieceAt(position);
        if (piece is not null)
        {
            _pieces.Remove(piece);
        }
    }

    public void AddMove(MoveDrawable moveDrawable)
    {
        _moves.Add(moveDrawable);
    }

    public void RemoveMove(MoveDrawable moveDrawable)
    {
        _moves.Remove(moveDrawable);
    }

    private readonly List<MoveDrawable> _moves = new();
}