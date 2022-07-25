using Chess.Core;
using Microsoft.Xna.Framework;
using Sandbox.Shared;

namespace Chess.View;

public class ChessGameMain : GameMain
{
    public readonly Board Board;
    private BoardView _boardView = null!;

    protected override void Initialize()
    {
        Graphics.IsFullScreen = false;
        Graphics.PreferredBackBufferWidth = 720;
        Graphics.PreferredBackBufferHeight = 720;
        Graphics.ApplyChanges();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        _boardView = new BoardView(Graphics.GraphicsDevice, Content, Board);

        OnGameStartRequested(new PlayerController(), new AiController());
    }

    private void OnGameStartRequested(AbstractBoardController white, AbstractBoardController black)
    {
        _boardView.SetWhitePlayer(white);
        _boardView.SetBlackPlayer(black);
        _boardView.StartGame();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DodgerBlue);

        base.Draw(gameTime);
        _boardView.Draw(gameTime);
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        _boardView.Update(gameTime);
    }

    public ChessGameMain(IReadOnlyList<string> launchArgs) : base(launchArgs)
    {
        Board = new Board();
        IsMouseVisible = true;
        IsFixedTimeStep = true;
    }
}