using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Checkers;

internal class BoardDrawer
{
    private static readonly Color WhiteCellColor = new(191, 176, 143);
    private static readonly Color BlackCellColor = new(36, 15, 14);
    private static readonly Color WhitePieceColor = new(191, 130, 38);
    private static readonly Color BlackPieceColor = new(99, 65, 40);
    private static readonly Color QueenInnerColor = new(138, 0, 14);
    private static readonly Color MoveColor = Color.SpringGreen;
    private static readonly Color CollidingMoveColor = Color.YellowGreen;
    private static readonly Color CapturedPreviewColor = new(0.2f, 0.2f, 0.2f, 0.25f);
    private static readonly Color SelectedCanMoveColor = new(0.1f, 0.5f, 0.1f, 0.2f);
    private static readonly Color SelectedCannotMoveColor = new(0.5f, 0.1f, 0.1f, 0.2f);
    private static readonly Color HintMoveColor = new(0, 50, 150);
    private static readonly Color HintMovePieceColor = new(0, 50, 150, 25);

    private readonly GraphicsDevice _device;

    private readonly SpriteFont _uiFont;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _cellTexture;
    private readonly Texture2D _pieceTexture;
    private readonly Texture2D _moveTexture;
    private readonly Texture2D _queenInnerTexture;

    private int _cellSize;
    private Board? _board;
    private IReadOnlyList<MoveFullInfo>? _moves;
    private IReadOnlyList<Move>? _collidingMoves;
    private int _pathMoved;
    private Position? _clickPosition;
    private Move? _hintMove;

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
        DrawPieces();
        DrawMoves();
        DrawHint();

        var gameEndState = _board.GetGameEndState();
        DrawGameEndStateText(gameEndState);

        _spriteBatch.End();
    }

    private void DrawHint()
    {
        if (_hintMove is null)
        {
            return;
        }

        if (_pathMoved == 0)
        {
            var position = _hintMove.PieceOnBoard.Position;
            var rect = new Rectangle(position.X * _cellSize, position.Y * _cellSize,
                _cellSize, _cellSize);
            _spriteBatch.Draw(_cellTexture, rect, HintMovePieceColor);
        }
        
        foreach (var position in _hintMove.Path.Skip(_pathMoved))
        {
            var moveRect = new Rectangle(position.X * _cellSize, position.Y * _cellSize,
                _cellSize, _cellSize);
            _spriteBatch.Draw(_moveTexture, moveRect, HintMoveColor);
        }
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
        if (_moves is null)
        {
            return;
        }

        foreach (var move in _moves)
        {
            var color = _collidingMoves?.Contains(move.Move) ?? false ? CollidingMoveColor : MoveColor;
            foreach (var position in move.Move.Path.Skip(_pathMoved))
            {
                var moveRect = new Rectangle(position.X * _cellSize, position.Y * _cellSize,
                    _cellSize, _cellSize);
                _spriteBatch.Draw(_moveTexture, moveRect, color);
            }
        }
    }

    private void DrawPieces()
    {
        Position? TryGetPartialMovePiecePosition(IEnumerable<PieceOnBoard> pieceOnBoards)
        {
            Position? result = null;
            if (_moves is null || _moves.Count == 0)
            {
                return result;
            }

            foreach (var pieceOnBoard in pieceOnBoards.Where(pieceOnBoard =>
                         _moves[0].StartPosition == pieceOnBoard.Position))
            {
                result = pieceOnBoard.Position;
                break;
            }

            return result;
        }

        var pieces = _board!.GetAllPieces().ToList();
        var capturedPositions = _moves is null
            ? new HashSet<Position>()
            : _moves.SelectMany(move => move.CapturedPositions).ToHashSet();

        Position? overridePosition = null;
        if (_pathMoved > 0)
        {
            overridePosition = TryGetPartialMovePiecePosition(pieces);
            var wasCaptured = capturedPositions.Take(_pathMoved).ToArray();
            pieces = pieces.Where(p =>
                    !wasCaptured.Contains(p.Position))
                .ToList();
        }

        Position? currentPartialMovePosition = overridePosition.HasValue ? _moves![0].Move.Path[_pathMoved - 1] : null;
        var willBeCaptured = capturedPositions.Skip(_pathMoved).ToArray();

        var hasSelectedPiece = IsSelectedAnyPiece();

        foreach (var pieceOnBoard in pieces)
        {
            var position = pieceOnBoard.Position;
            var shouldOverrideDrawAsQueen = false;
            if (overridePosition.HasValue && position == overridePosition.Value)
            {
                shouldOverrideDrawAsQueen = _moves is not null && _moves.Count > 0 &&
                                            position == _moves![0].StartPosition &&
                                            _pathMoved >= _moves[0].PromotionPathIndex;
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

        bool IsSelectedAnyPiece()
        {
            return _clickPosition.HasValue && _moves is not null &&
                   (!_board.GetPieceAt(_clickPosition.Value).IsEmpty
                    || _moves!.Any(m => m.Move.Path.Contains(_clickPosition.Value)));
        }

        if (!hasSelectedPiece)
        {
            return;
        }

        var canMove = _moves!.Count > 0;
        _spriteBatch.Draw(_cellTexture, GetCellRect(_clickPosition!.Value),
            canMove ? SelectedCanMoveColor : SelectedCannotMoveColor);
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

        var blackCellPositions = GetBlackCellPositions(_board!, _cellSize);
        foreach (var (x, y) in blackCellPositions)
        {
            var cellRect = new Rectangle(x, y, _cellSize, _cellSize);
            _spriteBatch.Draw(_cellTexture, cellRect, BlackCellColor);
        }

        foreach (var (x, y) in GetAllCellPositions(_board!, _cellSize))
        {
            var (xi, yi) = (x / _cellSize, y / _cellSize);
            var padding = 2f;
            var color = IsBlackCell(xi, yi) ? Color.White : Color.Black;
            _spriteBatch.DrawString(_uiFont, $"{xi}:{yi}", new Vector2(x + padding, y + padding),
                color, 0, Vector2.Zero,
                Vector2.One * 0.25f, SpriteEffects.None, 0);
        }
    }

    private static bool IsBlackCell(int xi, int yi)
    {
        return (xi + yi) % 2 == 1;
    }

    private static IEnumerable<(int, int)> GetBlackCellPositions(Board board, int cellSize)
    {
        foreach (var (cellX, cellY) in GetAllCellPositions(board, cellSize))
        {
            if (IsBlackCell(cellX / cellSize, cellY / cellSize))
            {
                yield return (cellX, cellY);
            }
        }
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

    public void SetAvailableMoves(IReadOnlyList<MoveFullInfo>? moves, int pathMoved = 0)
    {
        _moves = moves;
        _pathMoved = pathMoved;
    }

    public void SetCollidingMoves(IReadOnlyList<Move>? collisions)
    {
        _collidingMoves = collisions;
    }

    public void SetClickPosition(Position? position)
    {
        _clickPosition = position;
    }

    public void SetHintMove(Move? move)
    {
        _hintMove = move;
    }
}