using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Sandbox.Shared.Drawing;
using Sandbox.Shared.UI;

namespace Checkers.View;

public class PauseMenu
{
    private readonly DrawGroup _mainDrawGroup;

    private readonly GraphicsDevice _device;
    private readonly BoardView _boardView;

    private DrawGroup? _activeDrawGroup;

    private readonly UiLayout _mainLayout;

    public PauseMenu(GraphicsDevice device, BoardView boardView)
    {
        _device = device;
        _boardView = boardView;

        _mainDrawGroup = new DrawGroup(device);

        var screen = device.Viewport.Bounds;
        _mainLayout = new UiVerticalLayout
        {
            Bounds = screen,
            Enabled = false
        };

        CreateUiObjects();
    }

    private void CreateUiObjects()
    {
        var continueButton = new UiButton(_device, Color.OrangeRed, "Продолжить")
        {
            Width = 160,
            Height = 40,
            UiText = { FontScale = 0.35f }
        };
        var restartButton = new UiButton(_device, Color.OrangeRed, "Новая игра")
        {
            Width = 160,
            Height = 40,
            UiText = { FontScale = 0.35f }
        };
        var exitButton = new UiButton(_device, Color.OrangeRed, "Вернуться в меню")
        {
            Width = 160,
            Height = 40,
            UiText = { FontScale = 0.35f }
        };

        continueButton.Clicked += () =>
        {
            _mainLayout.Enabled = false;
            GameState.SwitchState(GameStateType.Board);
        };
        restartButton.Clicked += () =>
        {
            _mainLayout.Enabled = false;
            _boardView.StartGame();
            GameState.SwitchState(GameStateType.Board);
        };
        exitButton.Clicked += () =>
        {
            _mainLayout.Enabled = false;
            _boardView.EndGame();
            GameState.SwitchState(GameStateType.Menu);
        };

        _mainDrawGroup.AddDrawables(continueButton, restartButton, exitButton);
        _mainLayout.AddObjects(continueButton, restartButton, exitButton);
    }

    public void OpenMenu()
    {
        _mainLayout.Enabled = true;
        _activeDrawGroup = _mainDrawGroup;
    }

    public void Draw(GameTime gameTime)
    {
        _activeDrawGroup?.Draw(gameTime);
    }
}