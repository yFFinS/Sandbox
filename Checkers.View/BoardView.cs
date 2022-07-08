using Checkers.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Checkers.View;

public class ControllerVisitor
{
    public bool TurnPassPending { get; private set; }
    public bool GameRestartPending { get; private set; }
    public bool PauseMenuPending { get; private set; }
    public Move? PendingMove { get; private set; }

    public void PassTurn()
    {
        TurnPassPending = true;
    }

    public void RestartGame()
    {
        GameRestartPending = true;
    }

    public void MakeMove(Move move)
    {
        PendingMove = move;
    }

    public void OpenPauseMenu()
    {
        PauseMenuPending = true;
    }
}

public class BoardView
{
    private readonly BoardDrawer _boardDrawer;
    private readonly Board _board;

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

    public BoardView(GraphicsDevice device, Board board)
    {
        _board = board;

        _boardDrawable = new BoardDrawable(board, device.Viewport.Bounds);
        _boardDrawer = new BoardDrawer(device, _boardDrawable);
    }

    private void StartTurnAs(PieceColor color, MoveInfo lastMove)
    {
        if (color == PieceColor.White)
        {
            StartWhiteTurn(lastMove);
        }
        else
        {
            StartBlackTurn(lastMove);
        }
    }

    private void OnTurnPassed(PieceColor color)
    {
        _lastTurn = color == PieceColor.Black ? PieceColor.White : PieceColor.Black;
        StartTurnAs(_lastTurn, _lastMove!);
    }

    private void StartBlackTurn(MoveInfo lastMove)
    {
        _whitePlayer!.SetMyTurn(false, lastMove);
        _blackPlayer!.SetMyTurn(true, lastMove);
        Console.WriteLine("=== Starting Black's turn ===");
    }

    private void StartWhiteTurn(MoveInfo lastMove)
    {
        _blackPlayer!.SetMyTurn(false, lastMove);
        _whitePlayer!.SetMyTurn(true, lastMove);
        Console.WriteLine("=== Starting White's turn ===");
    }

    private MoveInfo? _lastMove;

    public void Update(GameTime gameTime)
    {
        if (!_isStarted || GameState.CurrentGameState != GameStateType.Board)
        {
            return;
        }

        var visitor = new ControllerVisitor();

        _whitePlayer!.Update(gameTime, visitor);
        _blackPlayer!.Update(gameTime, visitor);

        if (visitor.PendingMove.HasValue)
        {
            var move = visitor.PendingMove.Value;
            _lastMove = _board.MoveGenerator.GetMoveInfo(move);
            _board.MakeMove(move);
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
            GameState.SwitchState(GameStateType.Pause);
        }
    }

    public void StartGame()
    {
        _isStarted = true;
        
        _board.Reset();
        _boardDrawable.InitializeFromBoard(_board);
        _boardDrawable.ClearMoves();
        _lastTurn = _board.CurrentTurn;

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