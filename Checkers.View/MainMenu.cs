using System.Text.Json;
using Checkers.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Sandbox.Shared.Drawing;
using Sandbox.Shared.UI;

namespace Checkers.View;

public class MainMenu
{
    private readonly DrawGroup _mainDrawGroup;
    private readonly DrawGroup _playVsAiDrawGroup;

    private readonly GraphicsDevice _device;

    private DrawGroup? _activeDrawGroup;

    private readonly UiLayout _mainLayout;
    private readonly UiLayout _playVsAiLayout;

    public MainMenu(GraphicsDevice device)
    {
        _device = device;
        _mainDrawGroup = new DrawGroup(device);
        _playVsAiDrawGroup = new DrawGroup(device);

        var screen = device.Viewport.Bounds;
        _mainLayout = new UiVerticalLayout
        {
            Bounds = screen,
            Enabled = false
        };
        
        _playVsAiLayout = new UiVerticalLayout
        {
            Bounds = screen,
            Enabled = false
        };

        CreateUiObjects();
    }

    private PlayerType _whitePlayerType;
    private PlayerType _blackPlayerType;

    private PlayerType _playerType;
    private PlayerType _opponentType;

    public AbstractBoardController? WhitePlayer { get; private set; }
    public AbstractBoardController? BlackPlayer { get; private set; }

    private void CreateUiObjects()
    {
        var playVsAiButton = new UiButton(_device, Color.OrangeRed, "Против ПК")
        {
            Width = 160,
            Height = 40,
            UiText = { FontScale = 0.35f }
        };
        var playVsPlayerButton = new UiButton(_device, Color.OrangeRed, "Против друга")
        {
            Width = 160,
            Height = 40,
            UiText = { FontScale = 0.35f }
        };

        playVsAiButton.Clicked += OnPlayVsAiClicked;

        playVsPlayerButton.Clicked += OnPlayVsPlayerClicked;

        _mainDrawGroup.AddDrawables(playVsAiButton, playVsPlayerButton);
        _mainLayout.AddObjects(playVsAiButton, playVsPlayerButton);

        var playWhiteButton = new UiButton(_device, Color.OrangeRed, "Играть белыми")
        {
            Width = 160,
            Height = 40,
            UiText = { FontScale = 0.35f }
        };
        var playBlackButton = new UiButton(_device, Color.OrangeRed, "Играть черными")
        {
            Width = 160,
            Height = 40,
            UiText = { FontScale = 0.35f }
        };
        var backButton = new UiButton(_device, Color.OrangeRed, "Назад")
        {
            Width = 160,
            Height = 40,
            UiText = { FontScale = 0.35f }
        };

        playWhiteButton.Clicked += () =>
        {
            _whitePlayerType = _playerType;
            _blackPlayerType = _opponentType;
            _playVsAiLayout.Enabled = false;

            StartGame();
        };

        playBlackButton.Clicked += () =>
        {
            _whitePlayerType = _opponentType;
            _blackPlayerType = _playerType;
            _playVsAiLayout.Enabled = false;

            StartGame();
        };

        backButton.Clicked += () =>
        {
            _mainLayout.Enabled = true;
            _playVsAiLayout.Enabled = false;
            _activeDrawGroup = _mainDrawGroup;
        };

        _playVsAiDrawGroup.AddDrawables(playWhiteButton, playBlackButton, backButton);
        _playVsAiLayout.AddObjects(playWhiteButton, playBlackButton, backButton);
    }

    private void OnPlayVsPlayerClicked()
    {
        _mainLayout.Enabled = false;
        _whitePlayerType = PlayerType.Local;
        _blackPlayerType = PlayerType.Local;

        StartGame();
    }

    private void StartGame()
    {
        WhitePlayer = CreateController(_whitePlayerType);
        BlackPlayer = CreateController(_blackPlayerType);
        GameState.SwitchState(GameStateType.Board);
    }

    private static AbstractBoardController CreateController(PlayerType playerType)
    {
        return playerType switch
        {
            PlayerType.Ai => CreateAi(),
            PlayerType.Local => new PlayerController(),
            _ => throw new ArgumentOutOfRangeException(nameof(playerType), playerType, null)
        };
    }

    private static AiController CreateAi()
    {
        const string configPath = "ai_config.json";

        HeuristicAnalyzerConfig analyzerConfig;
        using (var stream = File.OpenRead(configPath))
        {
            var tempConfig = JsonSerializer.Deserialize<HeuristicAnalyzerConfig>(stream);

            analyzerConfig = tempConfig ?? throw new JsonException("Cannot load config.");
        }

        var ai = new AiController();
        ai.Analyzer.Configure(analyzerConfig);
        ai.Solver.Configure(config =>
        {
            config.MaxEvaluationTime = 1;
            config.MaxSearchDepth = 15;
        });

        return ai;
    }

    private void OnPlayVsAiClicked()
    {
        _playerType = PlayerType.Local;
        _opponentType = PlayerType.Ai;

        _activeDrawGroup = _playVsAiDrawGroup;
        _mainLayout.Enabled = false;
        _playVsAiLayout.Enabled = true;
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