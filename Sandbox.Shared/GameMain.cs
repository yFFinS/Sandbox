using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Sandbox.Shared.UI.Base;

namespace Sandbox.Shared;

public abstract class ApiManager
{
    public abstract void LoadAll(ContentManager contentManager);

    public abstract void Update(GameTime gameTime);
}

internal class DefaultApiManager : ApiManager
{
    private const string FontDirectory = "Fonts";
    private const string UiFontName = "UIFont";

    private InputApi _inputApi = null!;
    private UiManager _uiManager = null!;

    public override void LoadAll(ContentManager contentManager)
    {
        var fontApi = new FontApi();

        var uiFont = contentManager.Load<SpriteFont>(Path.Combine(FontDirectory, UiFontName));
        fontApi.AddFont(UiFontName, uiFont);
        fontApi.SetDefaultUiFont(UiFontName);

        Fonts.SetApi(fontApi);
        Input.SetApi(_inputApi = new InputApi());

        _uiManager = new UiManager();
    }

    public override void Update(GameTime gameTime)
    {
        _inputApi.Update(gameTime);
        _uiManager.Update(gameTime);
    }
}

public abstract class GameMain : Game
{
    protected readonly IReadOnlyList<string> LaunchArgs;
    protected readonly GraphicsDeviceManager Graphics;

    private ApiManager ApiManager { get; set; } = new DefaultApiManager();

    protected GameMain(IReadOnlyList<string> launchArgs)
    {
        LaunchArgs = launchArgs;
        Graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
    }

    protected override void LoadContent()
    {
        ApiManager.LoadAll(Content);
    }

    protected override void Update(GameTime gameTime)
    {
        ApiManager.Update(gameTime);
        base.Update(gameTime);
    }
}