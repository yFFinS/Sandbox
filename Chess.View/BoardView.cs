using Checkers.View;
using Chess.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Chess.View;

public class BoardView
{
    private readonly BoardDrawer _boardDrawer;
    private readonly ChessBoard _board;

    private AbstractBoardController? _whitePlayer;
    private AbstractBoardController? _blackPlayer;

    private PieceColor _lastTurn;
    private bool _isStarted;
    private readonly BoardDrawable _boardDrawable;

    public void SetWhitePlayer(AbstractBoardController controller)
    {
        _whitePlayer = controller;
        _whitePlayer.Initialize(_board, _boardDrawable);
    }

    public void SetBlackPlayer(AbstractBoardController controller)
    {
        _blackPlayer = controller;
        _blackPlayer.Initialize(_board, _boardDrawable);
    }

    public BoardView(GraphicsDevice device, ContentManager contentManager, ChessBoard board)
    {
        _board = board;

        _boardDrawable = new BoardDrawable(board, device.Viewport.Bounds);
        _boardDrawer = new BoardDrawer(device, contentManager, _boardDrawable);
    }

    private void StartTurnAs(PieceColor color)
    {
        if (color == PieceColor.White)
        {
            StartWhiteTurn();
        }
        else
        {
            StartBlackTurn();
        }
    }

    private void OnTurnPassed(PieceColor color)
    {
        _lastTurn = color == PieceColor.Black ? PieceColor.White : PieceColor.Black;
        StartTurnAs(_lastTurn);
    }

    private void StartBlackTurn()
    {
        _whitePlayer!.SetMyTurn(false);
        _blackPlayer!.SetMyTurn(true);
        Console.WriteLine("=== Starting Black's turn ===");
    }

    private void StartWhiteTurn()
    {
        _blackPlayer!.SetMyTurn(false);
        _whitePlayer!.SetMyTurn(true);
        Console.WriteLine("=== Starting White's turn ===");
    }

    private Move? _lastMove;

    public void Update(GameTime gameTime)
    {
        if (!_isStarted)
        {
            return;
        }

        var visitor = new ControllerVisitor();

        _whitePlayer!.Update(gameTime, visitor);
        _blackPlayer!.Update(gameTime, visitor);

        if (visitor.PendingMove.HasValue)
        {
            _lastMove = visitor.PendingMove;
            _board.MakeMove(_lastMove.Value);
            if (_board.IsCheck())
            {
                var kingPosition = _board.GetKingPosition(_board.ColorToMove);
                _boardDrawable.CheckPosition = kingPosition;
            }
            else
            {
                _boardDrawable.CheckPosition = -1;
            }
        }

        if (visitor.TurnPassPending)
        {
            OnTurnPassed(_lastTurn);
        }

        if (visitor.GameRestartPending)
        {
            StartGame();
            return;
        }

        if (visitor.PauseMenuPending)
        {
            //GameState.SwitchState(GameStateType.Pause);
        }
    }

    public void StartGame()
    {
        _isStarted = true;

        _board.ResetToDefaultPosition();
        _boardDrawable.InitializeFromBoard(_board);
        _boardDrawable.ClearMoves();
        _boardDrawable.CheckPosition = -1;
        _lastTurn = _board.ColorToMove;

        _whitePlayer!.StartGame(true, _blackPlayer is AiController ? PlayerType.Ai : PlayerType.Local);
        _blackPlayer!.StartGame(false, _whitePlayer is AiController ? PlayerType.Ai : PlayerType.Local);
    }

    public void EndGame()
    {
        _isStarted = false;
        _whitePlayer = null;
        _blackPlayer = null;
    }

    public void Draw(GameTime gameTime)
    {
        _boardDrawer.Draw(gameTime);
    }
}