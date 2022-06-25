using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Checkers;

public class GameMain : Game
{
    private readonly GraphicsDeviceManager _graphics;

    private Board _board = null!;
    private BoardView _boardView = null!;
    private InputApi _inputApi = null!;

    public GameMain(string[] args)
    {
        _graphics = new GraphicsDeviceManager(this);
        IsMouseVisible = true;
        Content.RootDirectory = "Content";
    }

    protected override void Initialize()
    {
        _graphics.IsFullScreen = false;
        _graphics.PreferredBackBufferWidth = 640;
        _graphics.PreferredBackBufferHeight = 640;
        _graphics.ApplyChanges();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        InitializeApis();


        _board = new Board(8);
        _boardView = new BoardView(_graphics.GraphicsDevice, _board);
    }

    private void InitializeApis()
    {
        var fontApi = new FontApi();

        const string uiFontName = "UIFont";
        var uiFont = Content.Load<SpriteFont>($"Fonts/{uiFontName}");
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