using Chess.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Sandbox.Shared;


namespace Chess.View;

public class BoardDrawer
{
    private readonly Texture2D _chessPiecesTexture;

    private readonly Dictionary<Piece, Rectangle> _chessTextureRegionMap = new();

    private readonly GraphicsDevice _device;

    private readonly SpriteFont _uiFont;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _cellTexture;
    private readonly Texture2D _moveTexture;

    private readonly BoardDrawable _boardDrawable;
    private readonly Texture2D _checkTexture;
    private readonly Texture2D _capturePreviewTexture;

    public BoardDrawer(GraphicsDevice device, ContentManager contentManager, BoardDrawable boardDrawable)
    {
        _device = device;
        _boardDrawable = boardDrawable;

        _uiFont = Fonts.DefaultUiFont;
        _spriteBatch = new SpriteBatch(_device);

        _cellTexture = TextureFactory.CreateFilledRectTexture(_device, Color.White);
        _moveTexture = TextureFactory.CreateFilledCircleTexture(_device, Color.White, 32, 256);
        _checkTexture = TextureFactory.CreateFilledCircleTexture(_device,
            new Color(255, 0, 0),
            new Color(255, 0, 0, 0),
            164, 256);

        _capturePreviewTexture = contentManager.Load<Texture2D>(Path.Combine("Sprites", "chess_capture"));
        _chessPiecesTexture = contentManager.Load<Texture2D>(Path.Combine("Sprites", "chess_pieces"));

        CreateChessTextureRegionMapping();
    }

    private void CreateChessTextureRegionMapping()
    {
        var currentRectangle = new Rectangle(0, 60, 60, 60);

        Rectangle Next()
        {
            var lastRectangle = currentRectangle;
            currentRectangle.Offset(60, 0);
            return lastRectangle;
        }

        // See Content/Sprites/chess_pieces.png

        _chessTextureRegionMap[new Piece(PieceColor.White, PieceType.Queen)] = Next();
        _chessTextureRegionMap[new Piece(PieceColor.White, PieceType.King)] = Next();
        _chessTextureRegionMap[new Piece(PieceColor.White, PieceType.Rook)] = Next();
        _chessTextureRegionMap[new Piece(PieceColor.White, PieceType.Knight)] = Next();
        _chessTextureRegionMap[new Piece(PieceColor.White, PieceType.Bishop)] = Next();
        _chessTextureRegionMap[new Piece(PieceColor.White, PieceType.Pawn)] = Next();

        currentRectangle = new Rectangle(0, 0, 60, 60);

        _chessTextureRegionMap[new Piece(PieceColor.Black, PieceType.Queen)] = Next();
        _chessTextureRegionMap[new Piece(PieceColor.Black, PieceType.King)] = Next();
        _chessTextureRegionMap[new Piece(PieceColor.Black, PieceType.Rook)] = Next();
        _chessTextureRegionMap[new Piece(PieceColor.Black, PieceType.Knight)] = Next();
        _chessTextureRegionMap[new Piece(PieceColor.Black, PieceType.Bishop)] = Next();
        _chessTextureRegionMap[new Piece(PieceColor.Black, PieceType.Pawn)] = Next();
    }

    public void Draw(GameTime gameTime)
    {
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

        DrawCells();
        DrawCellIndices(gameTime);

        if (_boardDrawable.CheckPosition != -1)
        {
            _spriteBatch.Draw(_checkTexture,
                _boardDrawable.GetCellRectangle(_boardDrawable.CheckPosition),
                Color.White);
        }

        DrawPieces();
        DrawMoves();
        DrawGameEndStateText(_boardDrawable.Board.GetGameEndState());

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
            new Rectangle(0, 0, (int) textSize.X, (int) textSize.Y), Color.Black);
        _spriteBatch.DrawString(_uiFont, text, position, Color.WhiteSmoke);
    }

    private void DrawMoves()
    {
        foreach (var moveDrawable in _boardDrawable.Moves.OrderBy(m => m.DrawOrder))
        {
            foreach (var path in moveDrawable.Move.GetPath().Skip(1))
            {
                var piece = _boardDrawable.GetPieceAt(path);
                var destinationRect = _boardDrawable.GetCellRectangle(path);
                _spriteBatch.Draw(piece is null ? _moveTexture : _capturePreviewTexture, destinationRect,
                    moveDrawable.PathColor);
            }

            // if (_boardDrawable.Board.IsEnPassant(moveDrawable.Move))
            // {
            //     var capturedPawn = _boardDrawable.Board.GetEnPassantCapturedPawn(moveDrawable.Move);
            //     var destinationRect = _boardDrawable.GetCellRectangle(capturedPawn.Position);
            //     _spriteBatch.Draw(_capturePreviewTexture, destinationRect, moveDrawable.PathColor);
            // }
        }
    }

    private void DrawPieces()
    {
        foreach (var pieceDrawable in _boardDrawable.Pieces.OrderBy(p => p.DrawOrder))
        {
            var destinationRect = _boardDrawable.GetCellRectangle(pieceDrawable.ScreenPosition);

            var piece = pieceDrawable.Piece;
            var key = new Piece(piece.Color, piece.Type);
            var sourceRect = _chessTextureRegionMap[key];
            _spriteBatch.Draw(_chessPiecesTexture, destinationRect, sourceRect, Color.White);
        }
    }

    private void DrawCells()
    {
        foreach (var cellDrawable in _boardDrawable.Cells)
        {
            _spriteBatch.Draw(_cellTexture, _boardDrawable.GetCellRectangle(cellDrawable.ScreenPosition),
                cellDrawable.Color);
        }
    }

    private void DrawCellIndices(GameTime gameTime)
    {
        foreach (var cellIndex in _boardDrawable.CellIndices)
        {
            cellIndex.Draw(gameTime, _spriteBatch);
        }
    }
}