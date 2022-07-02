using Checkers.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Sandbox.Shared;


namespace Checkers.View;

public class BoardDrawer
{
    private static readonly Color QueenInnerColor = new(138, 0, 14);
    private static readonly Color CapturedPreviewColor = new(0.2f, 0.2f, 0.2f, 0.25f);

    private readonly GraphicsDevice _device;

    private readonly SpriteFont _uiFont;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _cellTexture;
    private readonly Texture2D _pieceTexture;
    private readonly Texture2D _moveTexture;
    private readonly Texture2D _queenInnerTexture;

    private readonly BoardDrawable _boardDrawable;


    public BoardDrawer(GraphicsDevice device, BoardDrawable boardDrawable)
    {
        _device = device;
        _boardDrawable = boardDrawable;

        _uiFont = Fonts.DefaultUiFont;
        _spriteBatch = new SpriteBatch(_device);

        _cellTexture = TextureFactory.CreateFilledRectTexture(_device, Color.White);
        _pieceTexture = TextureFactory.CreateFilledCircleTexture(_device, Color.White, 86, 256);
        _moveTexture = TextureFactory.CreateFilledCircleTexture(_device, Color.White, 32, 256);
        _queenInnerTexture = TextureFactory.CreateFilledCircleTexture(_device, Color.White, 72, 256);
    }

    public void Draw(GameTime gameTime)
    {
        _spriteBatch.Begin();

        DrawCells();
        DrawCellIndices();
        DrawMoves();
        DrawPieces();

        var gameEndState = _boardDrawable.GetGameEndState();
        DrawGameEndStateText(gameEndState);

        _spriteBatch.End();
    }

    private void DrawGameEndStateText(GameEndState state)
    {
        if (state == GameEndState.None)
        {
            return;
        }

        var text = state switch
        {
            GameEndState.Draw => "НИЧЬЯ",
            GameEndState.WhiteWin => "БЕЛЫЕ ПОБЕДИЛИ",
            GameEndState.BlackWin => "ЧЕРНЫЕ ПОБЕДИЛИ",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

        var viewport = _device.Viewport;
        var screenCenter = new Vector2(viewport.Width, viewport.Height) / 2f;
        var textSize = _uiFont.MeasureString(text);
        var position = screenCenter - textSize / 2f;
        _spriteBatch.Draw(_cellTexture, position,
            new Rectangle(0, 0, (int)textSize.X, (int)textSize.Y), Color.Black);
        _spriteBatch.DrawString(_uiFont, text, position, Color.WhiteSmoke);
    }

    private void DrawMoves()
    {
        var visited = new HashSet<Position>();
        foreach (var moveDrawable in _boardDrawable.Moves.OrderByDescending(drawable => drawable.DrawOrder))
        {
            foreach (var position in moveDrawable.MoveInfo.Move.Path.Skip(moveDrawable.StaringIndex + 1)
                         .Where(pos => !visited.Contains(pos)))
            {
                visited.Add(position);
                var rect = _boardDrawable.GetCellRectangle(position);
                _spriteBatch.Draw(_moveTexture, rect, moveDrawable.PathColor);
            }
        }
    }

    private void DrawPieces()
    {
        foreach (var pieceDrawable in _boardDrawable.Pieces.OrderBy(p => p.DrawOrder))
        {
            var rect = _boardDrawable.GetCellRectangle(pieceDrawable.Position);

            var piece = pieceDrawable.Piece;
            var color = piece.Color == PieceColor.White ? PieceDrawable.WhitePieceColor : PieceDrawable.BlackPieceColor;
            _spriteBatch.Draw(_pieceTexture, rect, color);

            if (piece.Type == PieceType.Queen)
            {
                _spriteBatch.Draw(_queenInnerTexture, rect, QueenInnerColor);
            }

            if (pieceDrawable.PreviewCapture)
            {
                _spriteBatch.Draw(_cellTexture, rect, CapturedPreviewColor);
            }
        }
    }

    private void DrawCells()
    {
        foreach (var cellDrawable in _boardDrawable.Cells)
        {
            _spriteBatch.Draw(_cellTexture, _boardDrawable.GetCellRectangle(cellDrawable.Position), cellDrawable.Color);
        }
    }

    private void DrawCellIndices()
    {
        var cellSize = _boardDrawable.CellSize;
        var scale = cellSize / 256f;
        foreach (var cellDrawable in _boardDrawable.Cells)
        {
            const float padding = 2f;
            var (xi, yi) = (cellDrawable.BoardPosition.X, cellDrawable.BoardPosition.Y);

            var color = IsBlackCell(xi, yi) ? Color.White : Color.Black;
            _spriteBatch.DrawString(_uiFont, $"{xi}-{yi}", cellDrawable.Position + new Vector2(padding),
                color, 0, Vector2.Zero,
                Vector2.One * scale, SpriteEffects.None, 0);
        }
    }

    private static bool IsBlackCell(int xi, int yi)
    {
        return (xi + yi) % 2 == 1;
    }
}

internal static class EnumerableExtensions
{
    public static int IndexOf<TItem>(this IReadOnlyList<TItem> source, TItem item, int start = 0)
        where TItem : struct, IEquatable<TItem>
    {
        for (var i = start; i < source.Count; i++)
        {
            if (item.Equals(source[i]))
            {
                return i;
            }
        }

        return -1;
    }
}

public static class GameTimeExtensions
{
    public static float DeltaTime(this GameTime gameTime)
    {
        return (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000f;
    }
}