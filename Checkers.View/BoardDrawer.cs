using Checkers.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Checkers.View;

public class BoardDrawer
{
    private static readonly Color WhiteCellColor = new(191, 176, 143);
    private static readonly Color BlackCellColor = new(36, 15, 14);
    private static readonly Color WhitePieceColor = new(191, 130, 38);
    private static readonly Color BlackPieceColor = new(99, 65, 40);
    private static readonly Color QueenInnerColor = new(138, 0, 14);
    private static readonly Color CapturedPreviewColor = new(0.2f, 0.2f, 0.2f, 0.25f);

    private readonly GraphicsDevice _device;

    private readonly SpriteFont _uiFont;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _cellTexture;
    private readonly Texture2D _pieceTexture;
    private readonly Texture2D _moveTexture;
    private readonly Texture2D _queenInnerTexture;

    private int _cellSize;
    private Board? _board;
    private IReadOnlyList<MoveDisplayInfo>? _displayInfos;
    private int _partialPathIndex;

    public BoardDrawer(GraphicsDevice device)
    {
        _device = device;

        _uiFont = Fonts.DefaultUiFont;
        _spriteBatch = new SpriteBatch(_device);

        _cellTexture = TextureFactory.CreateFilledRectTexture(_device, Color.White);
        _pieceTexture = TextureFactory.CreateFilledCircleTexture(_device, Color.White, 86, 256);
        _moveTexture = TextureFactory.CreateFilledCircleTexture(_device, Color.White, 32, 256);
        _queenInnerTexture = TextureFactory.CreateFilledCircleTexture(_device, Color.White, 72, 256);
    }

    public void SetTargetBoard(Board board)
    {
        _board = board;
    }

    public void Draw(GameTime gameTime)
    {
        if (_board is null)
        {
            return;
        }

        _spriteBatch.Begin();

        DrawCells();
        DrawCellIndices();
        DrawPieces();
        DrawMoves();

        var gameEndState = _board.GetGameEndState();
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
            new Rectangle(0, 0, (int)textSize.X, (int)textSize.Y), Color.DimGray);
        _spriteBatch.DrawString(_uiFont, text, position, Color.Orange);
    }

    private void DrawMoves()
    {
        if (_displayInfos is null)
        {
            return;
        }

        var visited = new HashSet<Position>();
        foreach (var displayInfo in _displayInfos)
        {
            foreach (var position in displayInfo.FullInfo.Move.Path.Skip(_partialPathIndex)
                         .Where(pos => !visited.Contains(pos)))
            {
                visited.Add(position);
                _spriteBatch.Draw(_moveTexture, GetCellRect(position), displayInfo.PathColor);
            }
        }
    }

    private void DrawPieces()
    {
        Position? TryGetPartialMovePiecePosition(IEnumerable<PieceOnBoard> pieceOnBoards)
        {
            Position? result = null;
            if (_displayInfos is null || _displayInfos.Count == 0)
            {
                return result;
            }

            foreach (var pieceOnBoard in pieceOnBoards.Where(pieceOnBoard =>
                         _displayInfos[0].FullInfo.StartPosition == pieceOnBoard.Position))
            {
                result = pieceOnBoard.Position;
                break;
            }

            return result;
        }

        var pieces = _board!.GetAllPieces().ToList();
        var capturedPositions = _displayInfos is null
            ? new HashSet<Position>()
            : _displayInfos.SelectMany(info => info.FullInfo.CapturedPositions).ToHashSet();

        Position? overridePosition = null;
        if (_partialPathIndex > 0)
        {
            overridePosition = TryGetPartialMovePiecePosition(pieces);
            var wasCaptured = capturedPositions.Take(_partialPathIndex).ToArray();
            pieces = pieces.Where(p =>
                    !wasCaptured.Contains(p.Position))
                .ToList();
        }

        Position? currentPartialMovePosition = overridePosition.HasValue
            ? _displayInfos![0].FullInfo.Move.Path[_partialPathIndex - 1]
            : null;
        var willBeCaptured = capturedPositions.Skip(_partialPathIndex).ToArray();

        foreach (var pieceOnBoard in pieces)
        {
            var position = pieceOnBoard.Position;
            var shouldOverrideDrawAsQueen = false;
            if (overridePosition.HasValue && position == overridePosition.Value)
            {
                shouldOverrideDrawAsQueen = _displayInfos is not null && _displayInfos.Count > 0 &&
                                            position == _displayInfos![0].FullInfo.StartPosition &&
                                            _partialPathIndex > _displayInfos[0].FullInfo.PromotionPathIndex;
                position = currentPartialMovePosition!.Value;
            }

            var pieceRect = GetCellRect(position);

            var color = pieceOnBoard.Piece.Color == PieceColor.Black ? BlackPieceColor : WhitePieceColor;
            _spriteBatch.Draw(_pieceTexture, pieceRect, color);
            if (pieceOnBoard.Piece.Type == PieceType.Queen || shouldOverrideDrawAsQueen)
            {
                _spriteBatch.Draw(_queenInnerTexture, pieceRect, QueenInnerColor);
            }

            if (willBeCaptured.Contains(pieceOnBoard.Position))
            {
                _spriteBatch.Draw(_cellTexture, pieceRect, CapturedPreviewColor);
            }
        }
    }

    private Rectangle GetCellRect(Position position)
    {
        return new Rectangle(position.X * _cellSize, position.Y * _cellSize,
            _cellSize, _cellSize);
    }

    private void DrawCells()
    {
        var screenRect = new Rectangle(0, 0, _device.Viewport.Width, _device.Viewport.Height);
        _spriteBatch.Draw(_cellTexture, screenRect, WhiteCellColor);

        foreach (var (x, y) in GetAllCellPositions(_board!, _cellSize))
        {
            var cell = new Position(x / _cellSize, y / _cellSize);

            if (!_overridenCellColors.TryGetValue(cell, out var color))
            {
                if (!IsBlackCell(cell.X, cell.Y))
                {
                    continue;
                }

                color = BlackCellColor;
            }

            _spriteBatch.Draw(_cellTexture, GetCellRect(cell), color);
        }
    }

    private void DrawCellIndices()
    {
        var scale = _cellSize / 256f;
        foreach (var (x, y) in GetAllCellPositions(_board!, _cellSize))
        {
            const float padding = 2f;
            var (xi, yi) = (x / _cellSize, y / _cellSize);
            var color = IsBlackCell(xi, yi) ? Color.White : Color.Black;
            _spriteBatch.DrawString(_uiFont, $"{xi}-{yi}", new Vector2(x + padding, y + padding),
                color, 0, Vector2.Zero,
                Vector2.One * scale, SpriteEffects.None, 0);
        }
    }

    private Move? _animatingMove;
    
    public void PlayMoveAnimation(Move move)
    {
        _animatingMove = move;
    }

    private static bool IsBlackCell(int xi, int yi)
    {
        return (xi + yi) % 2 == 1;
    }

    private static IEnumerable<(int, int)> GetAllCellPositions(Board board, int cellSize)
    {
        for (var x = 0; x < board.Size; x++)
        {
            for (var y = 0; y < board.Size; y++)
            {
                yield return (x * cellSize, y * cellSize);
            }
        }
    }

    public void SetCellSize(int cellSize)
    {
        _cellSize = cellSize;
    }

    public void SetDisplayedMoves(IEnumerable<MoveDisplayInfo>? moves)
    {
        _displayInfos = moves?.OrderByDescending(info => info.DrawLayer).ToArray();
    }

    public void SetPartialPathIndex(int partialPathIndex)
    {
        _partialPathIndex = partialPathIndex + 1;
    }

    public void SetCellColor(Position cell, Color? color)
    {
        if (color is null)
        {
            _overridenCellColors.Remove(cell);
        }
        else
        {
            _overridenCellColors[cell] = color.Value;
        }
    }

    private readonly Dictionary<Position, Color> _overridenCellColors = new();

    public Position GetCellAt(Point screenPosition)
    {
        var x = screenPosition.X / _cellSize;
        var y = screenPosition.Y / _cellSize;
        return new Position(x, y);
    }
}