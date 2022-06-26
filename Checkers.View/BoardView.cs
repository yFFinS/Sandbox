using Checkers.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Checkers.View;

public class ControllerVisitor
{
    public bool TurnPassPending { get; private set; }
    public bool GameRestartPending { get; private set; }

    public void PassTurn()
    {
        TurnPassPending = true;
    }

    public void RestartGame()
    {
        GameRestartPending = true;
    }
}

public class BoardView
{
    private readonly GraphicsDevice _device;
    private readonly BoardDrawer _boardDrawer;
    private readonly Board _board;

    private readonly AbstractBoardController _whitePlayer;
    private readonly AbstractBoardController _blackPlayer;

    private PieceColor _lastTurn;

    public BoardView(GraphicsDevice device, Board board)
    {
        _device = device;
        _board = board;

        _boardDrawer = new BoardDrawer(device);
        _boardDrawer.SetTargetBoard(_board);

        var display = new BoardIntermediateDisplay(_board, _boardDrawer);

        _whitePlayer = new PlayerController();
        _whitePlayer.Initialize(_board, display);

        _blackPlayer = new AiController();
        _blackPlayer.Initialize(_board, display);

        UpdateCellSize();

        _lastTurn = _board.CurrentTurn;
        StartTurnAs(_lastTurn);
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
        _whitePlayer.SetMyTurn(false);
        _blackPlayer.SetMyTurn(true);
        Console.WriteLine("=== Starting Black's turn ===");
    }

    private void StartWhiteTurn()
    {
        _blackPlayer.SetMyTurn(false);
        _whitePlayer.SetMyTurn(true);
        Console.WriteLine("=== Starting White's turn ===");
    }

    private void UpdateCellSize()
    {
        var width = _device.Viewport.Width / _board.Size;
        var height = _device.Viewport.Height / _board.Size;
        var cellSize = Math.Min(width, height);
        _boardDrawer.SetCellSize(cellSize);
    }

    public void Update(GameTime gameTime)
    {
        var visitor = new ControllerVisitor();

        _whitePlayer.Update(gameTime, visitor);
        _blackPlayer.Update(gameTime, visitor);

        if (visitor.GameRestartPending)
        {
            _lastTurn = PieceColor.White;
            StartTurnAs(PieceColor.White);
        }
        else if (visitor.TurnPassPending)
        {
            OnTurnPassed(_lastTurn);
        }
    }

    public void Draw(GameTime gameTime)
    {
        _boardDrawer.Draw(gameTime);
    }
}