using Checkers.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Sandbox.Shared;

namespace Checkers.View;

public class CheckersGameMain : GameMain
{
    public readonly Board Board;
    private BoardView _boardView = null!;

    private AbstractBoardController? _blackPlayer;
    private AbstractBoardController? _whitePlayer;


    public void SetWhitePlayer(AbstractBoardController controller)
    {
        _whitePlayer = controller;
    }

    public void SetBlackPlayer(AbstractBoardController controller)
    {
        _blackPlayer = controller;
    }

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

        if (_whitePlayer is null || _blackPlayer is null)
        {
            throw new NullReferenceException();
        }

        _boardView = new BoardView(Graphics.GraphicsDevice, Board);
        _boardView.SetBlackPlayer(_blackPlayer);
        _boardView.SetWhitePlayer(_whitePlayer);
        _boardView.Start();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CadetBlue);
        _boardView.Draw(gameTime);

        base.Draw(gameTime);
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        _boardView.Update(gameTime);
    }

    public CheckersGameMain(IReadOnlyList<string> launchArgs) : base(launchArgs)
    {
        Board = new Board();
        IsMouseVisible = true;
        IsFixedTimeStep = true;
    }
}