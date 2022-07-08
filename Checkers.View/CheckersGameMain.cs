using Checkers.Core;
using Microsoft.Xna.Framework;
using Sandbox.Shared;

namespace Checkers.View;

public class CheckersGameMain : GameMain
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

        _mainMenu = new MainMenu(GraphicsDevice);
        _boardView = new BoardView(Graphics.GraphicsDevice, Board);
        _pauseMenu = new PauseMenu(GraphicsDevice, _boardView);

        GameState.StateChanged += (oldState, newState) =>
        {
            if (oldState == GameStateType.Menu && newState == GameStateType.Board)
            {
                var white = _mainMenu.WhitePlayer;
                var black = _mainMenu.BlackPlayer;
                if (white is null || black is null)
                {
                    throw new InvalidOperationException();
                }

                OnGameStartRequested(white, black);
            }

            if (newState == GameStateType.Menu)
            {
                _mainMenu.OpenMenu();
            }

            if (newState == GameStateType.Pause)
            {
                _pauseMenu.OpenMenu();
            }
        };
        
        GameState.SwitchState(GameStateType.Menu);
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

        switch (GameState.CurrentGameState)
        {
            case GameStateType.Menu:
                _mainMenu.Draw(gameTime);
                break;
            case GameStateType.Pause:
                _pauseMenu.Draw(gameTime);
                break;
            case GameStateType.Board:
                _boardView.Draw(gameTime);
                break;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        switch (GameState.CurrentGameState)
        {
            case GameStateType.Menu:
                break;
            case GameStateType.Pause:
                break;
            case GameStateType.Board:
                _boardView.Update(gameTime);
                break;
        }
    }

    public CheckersGameMain(IReadOnlyList<string> launchArgs) : base(launchArgs)
    {
        Board = new Board();
        IsMouseVisible = true;
        IsFixedTimeStep = true;
    }

    private MainMenu _mainMenu = null!;
    private PauseMenu _pauseMenu = null!;
}