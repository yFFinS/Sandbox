using Checkers.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Checkers.View;

public class GameMain : Game
{
    private readonly GraphicsDeviceManager _graphics;

    private readonly Board _board;

    private BoardView _boardView = null!;
    private InputApi _inputApi = null!;

    private AbstractBoardController? _blackPlayer;
    private AbstractBoardController? _whitePlayer;

    public GameMain(IReadOnlyList<string> args)
    {
        _graphics = new GraphicsDeviceManager(this);

        IsMouseVisible = true;
        Content.RootDirectory = "Content";

        var boardSize = Board.StandardSize;
        if (args.Count == 1 && int.TryParse(args[0], out var argBoardSize))
        {
            if (Board.IsValidBoardSize(argBoardSize))
            {
                boardSize = argBoardSize;
            }
        }

        _board = new Board(boardSize);
    }


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
        _graphics.IsFullScreen = false;
        _graphics.PreferredBackBufferWidth = 720;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        InitializeApis();

        if (_whitePlayer is null || _blackPlayer is null)
        {
            throw new NullReferenceException();
        }

        _boardView = new BoardView(_graphics.GraphicsDevice, _board);
        _boardView.SetBlackPlayer(_blackPlayer);
        _boardView.SetWhitePlayer(_whitePlayer);
        _boardView.Start();
    }

    private void InitializeApis()
    {
        var fontApi = new FontApi();

        const string uiFontName = "UIFont";
        var uiFont = Content.Load<SpriteFont>(Path.Combine("Fonts", uiFontName));
        fontApi.AddFont(uiFontName, uiFont);
        fontApi.SetDefaultUiFont(uiFontName);

        Fonts.SetApi(fontApi);

        _inputApi = new InputApi();
        Input.SetApi(_inputApi);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CadetBlue);

        _boardView.Draw(gameTime);
        base.Draw(gameTime);
    }

    protected override void Update(GameTime gameTime)
    {
        _inputApi.Update(gameTime);

        _boardView.Update(gameTime);
        base.Update(gameTime);
    }
}